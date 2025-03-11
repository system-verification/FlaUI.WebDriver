using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using FlaUI.Core.Input;
using FlaUI.WebDriver.Models;
using Microsoft.Extensions.Logging;

namespace FlaUI.WebDriver.Services
{
    public class ActionsDispatcher : IActionsDispatcher
    {
        private readonly ILogger<ActionsDispatcher> _logger;

        public ActionsDispatcher(ILogger<ActionsDispatcher> logger)
        {
            _logger = logger;
        }

        public async Task DispatchAction(Session session, Action action)
        {
            switch (action.Type)
            {
                case "pointer":
                    await DispatchPointerAction(session, action);
                    break;
                case "key":
                    await DispatchKeyAction(session, action);
                    break;
                case "wheel":
                    await DispatchWheelAction(session, action);
                    break;
                case "none":
                    await DispatchNullAction(session, action);
                    break;
                default:
                    throw WebDriverResponseException.UnsupportedOperation($"Action type {action.Type} not supported");
            }
        }

        /// <summary>
        /// Implements "dispatch actions for a string" (WebDriver's send-keys).
        /// </summary>
        public async Task DispatchActionsForString(Session session, string inputId, KeyInputSource source, string text)
        {
            var clusters = StringInfo.GetTextElementEnumerator(text);
            var currentTypeableText = new StringBuilder();

            while (clusters.MoveNext())
            {
                var cluster = clusters.GetTextElement();
                _logger.LogDebug("Cluster: {cluster}", cluster);

                if (cluster == Keys.Null.ToString())
                {
                    if (currentTypeableText.Length > 0)
                    {
                        await DispatchTypeableText(session, inputId, source, currentTypeableText.ToString());
                        currentTypeableText.Clear();
                    }
                    // Ensure all modifiers are released.
                    await ClearModifierKeyState(session, inputId);
                }
                else if (Keys.IsModifier(Keys.GetNormalizedKeyValue(cluster)))
                {
                    if (currentTypeableText.Length > 0)
                    {
                        await DispatchTypeableText(session, inputId, source, currentTypeableText.ToString());
                        currentTypeableText.Clear();
                    }

                    // Immediately dispatch the modifier key event.
                    var modAction = new Action(
                        new ActionSequence { Id = inputId, Type = "key" },
                        new ActionItem { Type = "keyDown", Value = cluster }
                    );
                    await DispatchAction(session, modAction);
                }
                else 
                {
                    currentTypeableText.Append(cluster);
                }
            }

            if (currentTypeableText.Length > 0)
            {
                _logger.LogDebug("Flushing final typeable text: {text}", currentTypeableText);
                await DispatchTypeableText(session, inputId, source, currentTypeableText.ToString());
            }

            await ClearModifierKeyState(session, inputId);
        }

        /// <summary>
        /// Converts the string into a complete batch of key actions.
        /// This method builds keyDown/keyUp events for every character,
        /// toggling the Shift modifier inline when required.
        /// </summary>
        private async Task DispatchTypeableText(Session session, string inputId, KeyInputSource source, string text)
        {
            _logger.LogDebug("Dispatching typeable text in batch: {text} (length: {len})", text, text.Length);
            var actions = new List<Action>();

            // Group characters by the shift requirement.
            var currentSegment = new StringBuilder();
            bool? currentNeedsShift = null;

            foreach (char c in text)
            {
                bool needsShift = Keys.IsShiftedChar(c);
                if (currentNeedsShift == null)
                {
                    currentNeedsShift = needsShift;
                }

                // Flush the segment if the shift state has changed.
                if (currentNeedsShift != needsShift)
                {
                    AppendSegmentActions(actions, inputId, currentSegment.ToString(), currentNeedsShift.Value, source);
                    currentSegment.Clear();
                    currentNeedsShift = needsShift;
                }
                currentSegment.Append(c);
            }
            // Flush final segment.
            if (currentSegment.Length > 0 && currentNeedsShift != null)
            {
                AppendSegmentActions(actions, inputId, currentSegment.ToString(), currentNeedsShift.Value, source);
            }

            // Dispatch the entire batch with a slight delay between each action.
            foreach (var act in actions)
            {
                await DispatchAction(session, act);
                await Task.Delay(50); // 10ms delay between keystroke events
            }
            
            // Allow events to settle before clearing any modifiers.
            await Task.Delay(150);

            // Explicitly release Shift if still pressed.
            if (source.Shift)
            {
                await DispatchAction(session, new Action(
                    new ActionSequence { Id = inputId, Type = "key" },
                    new ActionItem { Type = "keyUp", Value = Keys.LeftShift.ToString() }
                ));
                source.Shift = false;
            }
            
            _logger.LogDebug("Completed processing batch typeable text: {text}", text);
            await Task.CompletedTask;
        }

        private void AppendSegmentActions(List<Action> actions, string inputId, string segment, bool needsShift, KeyInputSource source)
        {
            // If the current input state doesn't match the segment, push shift toggle actions.
            // For example, if the segment needs shift and it's not on, press shift first.
            if (needsShift && !source.Shift)
            {
                actions.Add(new Action(
                    new ActionSequence { Id = inputId, Type = "key" },
                    new ActionItem { Type = "keyDown", Value = Keys.LeftShift.ToString() }
                ));
                source.Shift = true;
            }

            // For the segment, add one keyDown/keyUp pair for each character.
            foreach (char c in segment)
            {
                // Convert to lowercase if Shift is needed, so the OS combines Shift + lowercase
                var charToSend = needsShift && char.IsUpper(c)
                    ? char.ToLower(c, CultureInfo.InvariantCulture)
                    : c;

                actions.Add(new Action(
                    new ActionSequence { Id = inputId, Type = "key" },
                    new ActionItem { Type = "keyDown", Value = charToSend.ToString() }
                ));
                actions.Add(new Action(
                    new ActionSequence { Id = inputId, Type = "key" },
                    new ActionItem { Type = "keyUp", Value = charToSend.ToString() }
                ));
            }

            // Always issue a SHIFT keyUp if this segment required shift.
            if (needsShift)
            {
                actions.Add(new Action(
                    new ActionSequence { Id = inputId, Type = "key" },
                    new ActionItem { Type = "keyUp", Value = Keys.LeftShift.ToString() }
                ));
                source.Shift = false;
            }
        }

        /// <summary>
        /// Releases any modifier keys that remain pressed.
        /// </summary>
        public async Task DispatchReleaseActions(Session session, string inputId)
        {
            var source = session.InputState.GetInputSource<KeyInputSource>(inputId);
            for (int i = session.InputState.InputCancelList.Count - 1; i >= 0; i--)
            {
                var cancelAction = session.InputState.InputCancelList[i];
                if (cancelAction.Id == inputId && cancelAction.Value != null)
                {
                    string normalizedKey = Keys.GetNormalizedKeyValue(cancelAction.Value);
                    if ((normalizedKey == "Alt" || normalizedKey == "Shift" || normalizedKey == "Control" || normalizedKey == "Meta")
                        && source != null && source.Pressed.Contains(cancelAction.Value))
                    {
                        await DispatchAction(session, cancelAction);
                    }
                }
                session.InputState.InputCancelList.RemoveAt(i);
            }
        }

        /// <summary>
        /// Clears residual modifier key state.
        /// </summary>
        private Task ClearModifierKeyState(Session session, string inputId) 
            => DispatchReleaseActions(session, inputId);

        private static async Task DispatchNullAction(Session session, Action action)
        {
            if (action.SubType == "pause")
            {
                await Task.Yield();
            }
            else
            {
                throw WebDriverResponseException.InvalidArgument($"Null action subtype {action.SubType} unknown");
            }
        }

        /// <summary>
        /// Dispatches keyDown, keyUp or pause events per WebDriver spec.
        /// </summary>
        private async Task DispatchKeyAction(Session session, Action action)
        {
            if (action.Value == null)
            {
                return;
            }

            var source = session.InputState.GetInputSource<KeyInputSource>(action.Id)
                ?? throw WebDriverResponseException.UnknownError($"Input source for key action '{action.Id}' not found.");

            switch (action.SubType)
            {
                case "keyDown":
                    {
                        var key = Keys.GetNormalizedKeyValue(action.Value);
                        var code = Keys.GetCode(action.Value);
                        var virtualKey = Keys.GetVirtualKey(code.ToString());
                        _logger.LogDebug("Dispatching key down action, key '{Value}' with ID '{Id}'", code, action.Id);

                        if (key == "Alt")
                        {
                            source.Alt = true;
                        }
                        else if (key == "Shift")
                        {
                            source.Shift = true;
                        }
                        else if (key == "Control")
                        {
                            source.Ctrl = true;
                        }
                        else if (key == "Meta")
                        {
                            source.Meta = true;
                        }
                        source.Pressed.Add(action.Value);
                        Keyboard.Press(virtualKey);

                        // Immediately queue a keyUp cancel action.
                        var cancelAction = action.Clone();
                        cancelAction.SubType = "keyUp";
                        session.InputState.InputCancelList.Add(cancelAction);
                        break;
                    }
                case "keyUp":
                    {
                        var key = Keys.GetNormalizedKeyValue(action.Value);
                        var code = Keys.GetCode(action.Value);
                        var virtualKey = Keys.GetVirtualKey(code.ToString());
                        _logger.LogDebug("Dispatching key up action, key '{Value}' with ID '{Id}'", code, action.Id);

                        if (key == "Alt")
                        {
                            source.Alt = false;
                        }
                        else if (key == "Shift")
                        {
                            source.Shift = false;
                        }
                        else if (key == "Control")
                        {
                            source.Ctrl = false;
                        }
                        else if (key == "Meta")
                        {
                            source.Meta = false;
                        }
                        source.Pressed.Remove(action.Value);
                        Keyboard.Release(virtualKey);
                        break;
                    }
                case "pause":
                    await Task.Yield();
                    break;
                default:
                    throw WebDriverResponseException.InvalidArgument($"Key action subtype {action.SubType} unknown");
            }
            await Task.CompletedTask;
        }

        private async Task DispatchWheelAction(Session session, Action action)
        {
            switch (action.SubType)
            {
                case "scroll":
                    _logger.LogDebug("Dispatching wheel scroll action, coordinates ({X},{Y}), delta ({DeltaX},{DeltaY}) with ID '{Id}'",
                        action.X, action.Y, action.DeltaX, action.DeltaY, action.Id);
                    if (action.X == null || action.Y == null)
                    {
                        throw WebDriverResponseException.InvalidArgument("For wheel scroll, X and Y are required");
                    }
                    Mouse.MoveTo(action.X.Value, action.Y.Value);

                    // Use GetValueOrDefault() to safely handle nullable values.
                    if (action.DeltaY.GetValueOrDefault() != 0)
                    {
                        Mouse.Scroll(action.DeltaY.GetValueOrDefault());
                    }
                    if (action.DeltaX.GetValueOrDefault() != 0)
                    {
                        Mouse.HorizontalScroll(action.DeltaX.GetValueOrDefault());
                    }
                    break;
                case "pause":
                    await Task.Yield();
                    break;
                default:
                    throw WebDriverResponseException.InvalidArgument($"Wheel action subtype {action.SubType} unknown");
            }
            await Task.CompletedTask;
        }

        private async Task DispatchPointerAction(Session session, Action action)
        {
            switch (action.SubType)
            {
                case "pointerMove":
                    _logger.LogDebug("Dispatching pointer move action, coordinates ({X},{Y}) with origin {Origin}, with ID '{Id}'",
                        action.X, action.Y, action.Origin, action.Id);
                    var point = GetCoordinates(session, action);
                    Mouse.MoveTo(point);
                    break;
                case "pointerDown":
                    _logger.LogDebug("Dispatching pointer down action, button {Button}, with ID '{Id}'", action.Button, action.Id);
                    Mouse.Down(GetMouseButton(action.Button));
                    var cancelAction = action.Clone();
                    cancelAction.SubType = "pointerUp";
                    session.InputState.InputCancelList.Add(cancelAction);
                    break;
                case "pointerUp":
                    _logger.LogDebug("Dispatching pointer up action, button {Button}, with ID '{Id}'", action.Button, action.Id);
                    Mouse.Up(GetMouseButton(action.Button));
                    break;
                case "pause":
                    await Task.Yield();
                    break;
                default:
                    throw WebDriverResponseException.UnsupportedOperation($"Pointer action subtype {action.SubType} not supported");
            }
            await Task.CompletedTask;
        }

        private static Point GetCoordinates(Session session, Action action)
        {
            var origin = action.Origin ?? "viewport";
            switch (origin)
            {
                case "viewport":
                    {
                        // Ensure X and Y are non-null.
                        int x = action.X ?? throw WebDriverResponseException.InvalidArgument("For pointer move, X is required");
                        int y = action.Y ?? throw WebDriverResponseException.InvalidArgument("For pointer move, Y is required");
                        return new Point(x, y);
                    }
                case "pointer":
                    {
                        int x = action.X ?? throw WebDriverResponseException.InvalidArgument("For pointer move, X is required");
                        int y = action.Y ?? throw WebDriverResponseException.InvalidArgument("For pointer move, Y is required");
                        var current = Mouse.Position;
                        return new Point(current.X + x, current.Y + y);
                    }
                case Dictionary<string, string> originMap:
                    {
                        if (originMap.TryGetValue("element-6066-11e4-a52e-4f735466cecf", out var elementId))
                        {
                            if (session.FindKnownElementById(elementId) is { } element)
                            {
                                var bounds = element.BoundingRectangle;
                                int x = bounds.Left + (bounds.Width / 2) + action.X.GetValueOrDefault();
                                int y = bounds.Top + (bounds.Height / 2) + action.Y.GetValueOrDefault();
                                return new Point(x, y);
                            }
                            throw WebDriverResponseException.InvalidArgument($"Unknown element ID '{elementId}' provided for action item '{action.Type}'.");
                        }
                        throw WebDriverResponseException.InvalidArgument($"Unknown element '{origin}' provided for action item '{action.Type}'.");
                    }
                default:
                    throw WebDriverResponseException.InvalidArgument($"Unknown origin type '{origin}' provided for action item '{action.Type}'.");
            }
        }

        private static MouseButton GetMouseButton(int? button)
        {
            if (button == null)
                throw WebDriverResponseException.InvalidArgument("Pointer action button argument missing");
            return button switch
            {
                0 => MouseButton.Left,
                1 => MouseButton.Middle,
                2 => MouseButton.Right,
                3 => MouseButton.XButton1,
                4 => MouseButton.XButton2,
                _ => throw WebDriverResponseException.UnsupportedOperation($"Pointer button {button} not supported")
            };
        }
    }
}
