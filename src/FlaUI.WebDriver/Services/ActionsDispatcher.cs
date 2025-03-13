using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FlaUI.Core.Input;
using FlaUI.WebDriver.Models;
using FlaUI.Core.WindowsAPI;

namespace FlaUI.WebDriver.Services
{
    public class ActionsDispatcher : IActionsDispatcher
    {
        private readonly ILogger<ActionsDispatcher> _logger;
        // Semaphore to enforce sequential processing of actions.
        //private readonly SemaphoreSlim _actionLock = new SemaphoreSlim(1, 1);

        private readonly IBatchInputDispatcher _batchDispatcher;

        public ActionsDispatcher(ILogger<ActionsDispatcher> logger, IBatchInputDispatcher batchDispatcher)
        {
            _logger = logger;
            _batchDispatcher = batchDispatcher;
        }

        /// <summary>
        /// Public method to process a single action.
        /// This method acquires the semaphore so it is not reentrant.
        /// </summary>
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
        /// Forms the full list of actions for a sendKeys request and then processes them sequentially.
        /// No calls to DispatchAction are nested.
        /// </summary>
        public async Task DispatchActionsForString(Session session, string inputId, KeyInputSource source, string text)
        {
            var actionsToProcess = new List<Action>();
            var clusters = StringInfo.GetTextElementEnumerator(text);
            var currentTypeableText = new StringBuilder();

            while (clusters.MoveNext())
            {
                var cluster = clusters.GetTextElement();
                //_logger.LogDebug("Cluster: {cluster}", cluster);

                if (cluster == Keys.Null.ToString())
                {
                    if (currentTypeableText.Length > 0)
                    {
                        actionsToProcess.AddRange(BuildTypeableTextActions(inputId, source, currentTypeableText.ToString()));
                        currentTypeableText.Clear();
                    }
                    // Queue modifier releases
                    actionsToProcess.AddRange(BuildModifierReleaseActions(session, inputId));
                }
                else if (Keys.IsModifier(Keys.GetNormalizedKeyValue(cluster)))
                {
                    if (currentTypeableText.Length > 0)
                    {
                        actionsToProcess.AddRange(BuildTypeableTextActions(inputId, source, currentTypeableText.ToString()));
                        currentTypeableText.Clear();
                    }
                    // Queue a modifier keyDown and pair it with keyUp later.
                    actionsToProcess.Add(new Action(
                        new ActionSequence { Id = inputId, Type = "key" },
                        new ActionItem { Type = "keyDown", Value = cluster }
                    ));
                    actionsToProcess.Add(new Action(
                        new ActionSequence { Id = inputId, Type = "key" },
                        new ActionItem { Type = "keyUp", Value = cluster }
                    ));
                }
                else
                {
                    currentTypeableText.Append(cluster);
                }
            }

            if (currentTypeableText.Length > 0)
            {
                _logger.LogDebug("Flushing final typeable text: {text}", currentTypeableText);
                actionsToProcess.AddRange(BuildTypeableTextActions(inputId, source, currentTypeableText.ToString()));
            }

            _logger.LogDebug("Completed processing batch typeable text: {text}", text);
            // Queue any remaining modifier release actions.
            actionsToProcess.AddRange(BuildModifierReleaseActions(session, inputId));

            // Process actions sequentially (each call to DispatchAction obtains the semaphore).
            await ProcessActionsQueue(session, actionsToProcess);
        }

        public void DispatchActionsForStringSync(Session session, string inputId, KeyInputSource source, string text)
        {
            var actionsToProcess = new List<Action>();
            var clusters = StringInfo.GetTextElementEnumerator(text);
            var currentTypeableText = new StringBuilder();

            while (clusters.MoveNext())
            {
                var cluster = clusters.GetTextElement();
                //_logger.LogDebug("Cluster: {cluster}", cluster);

                if (cluster == Keys.Null.ToString())
                {
                    if (currentTypeableText.Length > 0)
                    {
                        actionsToProcess.AddRange(BuildTypeableTextActions(inputId, source, currentTypeableText.ToString()));
                        currentTypeableText.Clear();
                    }
                    // Queue modifier releases.
                    actionsToProcess.AddRange(BuildModifierReleaseActions(session, inputId));
                }
                else if (Keys.IsModifier(Keys.GetNormalizedKeyValue(cluster)))
                {
                    if (currentTypeableText.Length > 0)
                    {
                        actionsToProcess.AddRange(BuildTypeableTextActions(inputId, source, currentTypeableText.ToString()));
                        currentTypeableText.Clear();
                    }
                    // Queue the modifier keyDown and its corresponding keyUp.
                    actionsToProcess.Add(new Action(
                        new ActionSequence { Id = inputId, Type = "key" },
                        new ActionItem { Type = "keyDown", Value = cluster }
                    ));
                    actionsToProcess.Add(new Action(
                        new ActionSequence { Id = inputId, Type = "key" },
                        new ActionItem { Type = "keyUp", Value = cluster }
                    ));
                }
                else
                {
                    currentTypeableText.Append(cluster);
                }
            }

            if (currentTypeableText.Length > 0)
            {
                _logger.LogDebug("Flushing final typeable text: {text}", currentTypeableText);
                actionsToProcess.AddRange(BuildTypeableTextActions(inputId, source, currentTypeableText.ToString()));
            }

            _logger.LogDebug("Completed processing batch typeable text: {text}", text);

            ProcessActionsQueueSync(session, actionsToProcess);
        }

        public void DispatchKeysViaBatchInput(string text)
        {
            var keyCodes = new List<ushort>();
            foreach (char c in text)
            {
                // Use the character directly (as it’s already a one‐character string).
                string key = c.ToString();
                ushort vk = (ushort)Keys.GetVirtualKey(key);
                keyCodes.Add(vk);
            }
            _batchDispatcher.SendKeysBatch(keyCodes.ToArray());
        }

        /// <summary>
        /// Process a list of actions sequentially.
        /// </summary>
        private async Task ProcessActionsQueue(Session session, List<Action> actions)
        {
            _logger.LogDebug("Async Processing {ActionCount} queued actions", actions.Count);
            foreach (var act in actions)
            {
                await DispatchAction(session, act);
            }
            _logger.LogDebug("Dispatching final pause action to flush pending input events.");
            await DispatchAction(session, new Action(
                    new ActionSequence { Id = "flush", Type = "key" },
                    new ActionItem { Type = "pause", Value = null }
                ));
        }

        /// <summary>
        /// Synchronously processes the given list of actions. Each action is dispatched using a blocking call.
        /// </summary>
        private void ProcessActionsQueueSync(Session session, List<Action> actions)
        {
            _logger.LogDebug("Sync processing {ActionCount} queued actions", actions.Count);
            foreach (var act in actions)
            {
                DispatchAction(session, act).GetAwaiter().GetResult();
                // Flush the system input by waiting briefly.
                System.Threading.Thread.Sleep(1);
            }
            DispatchAction(session, new Action(
                new ActionSequence { Id = "flush", Type = "key" },
                new ActionItem { Type = "pause", Value = null }
            )).GetAwaiter().GetResult();
            // Flush the system input by waiting briefly.
            System.Threading.Thread.Sleep(1);
        }

        /// <summary>
        /// Builds actions for sending a string chunk.
        /// Groups characters by whether they need Shift.
        /// </summary>
        private List<Action> BuildTypeableTextActions(string inputId, KeyInputSource source, string text)
        {
            var actions = new List<Action>();
            var currentSegment = new StringBuilder();
            bool? currentNeedsShift = null;

            foreach (char c in text)
            {
                bool needsShift = Keys.IsShiftedChar(c);
                if (currentNeedsShift == null)
                    currentNeedsShift = needsShift;

                if (currentNeedsShift != needsShift)
                {
                    AppendSegmentActions(actions, inputId, currentSegment.ToString(), currentNeedsShift.Value, source);
                    currentSegment.Clear();
                    currentNeedsShift = needsShift;
                }
                currentSegment.Append(c);
            }
            if (currentSegment.Length > 0 && currentNeedsShift != null)
            {
                AppendSegmentActions(actions, inputId, currentSegment.ToString(), currentNeedsShift.Value, source);
            }
            return actions;
        }

        /// <summary>
        /// Builds actions to release any modifiers that remain pressed.
        /// </summary>
        private List<Action> BuildModifierReleaseActions(Session session, string inputId)
        {
            var actions = new List<Action>();
            var source = session.InputState.GetInputSource<KeyInputSource>(inputId);
            if (source != null)
            {
                if (source.Shift)
                {
                    actions.Add(new Action(
                        new ActionSequence { Id = inputId, Type = "key" },
                        new ActionItem { Type = "keyUp", Value = Keys.LeftShift.ToString() }
                    ));
                    source.Shift = false;
                }
                if (source.Alt)
                {
                    actions.Add(new Action(
                        new ActionSequence { Id = inputId, Type = "key" },
                        new ActionItem { Type = "keyUp", Value = "Alt" }
                    ));
                    source.Alt = false;
                }
                if (source.Ctrl)
                {
                    actions.Add(new Action(
                        new ActionSequence { Id = inputId, Type = "key" },
                        new ActionItem { Type = "keyUp", Value = "Control" }
                    ));
                    source.Ctrl = false;
                }
                if (source.Meta)
                {
                    actions.Add(new Action(
                        new ActionSequence { Id = inputId, Type = "key" },
                        new ActionItem { Type = "keyUp", Value = "Meta" }
                    ));
                    source.Meta = false;
                }
            }
            return actions;
        }

        /// <summary>
        /// Helper: add keyDown/keyUp pairs for a text segment.
        /// It groups characters that need Shift and adds a Shift press/release around them.
        /// </summary>
        private void AppendSegmentActions(List<Action> actions, string inputId, string segment, bool needsShift, KeyInputSource source)
        {
            // If the segment requires Shift, press it once before the segment.
            if (needsShift)
            {
                actions.Add(new Action(
                    new ActionSequence { Id = inputId, Type = "key" },
                    new ActionItem { Type = "keyDown", Value = Keys.LeftShift }
                ));
                source.Shift = true;
            }

            foreach (char c in segment)
            {
                // Determine if this character requires additional modifiers.
                short vkScan = Keys.GetVkScanEx(c, Keys.GetKeyboardLayout(0));
                // The high-order byte indicates the modifier state: 1 = Shift, 2 = Ctrl, 4 = Alt.
                byte modifiers = (byte)((vkScan >> 8) & 0xFF);
                bool requiresCtrl = (modifiers & 2) != 0;
                bool requiresAlt = (modifiers & 4) != 0;
                bool needsAltGr = requiresCtrl && requiresAlt; // Typically AltGr
                
                // If AltGr is needed (Control+Alt) and not already pressed, press them.
                if (needsAltGr && !source.Ctrl && !source.Alt)
                {
                    actions.Add(new Action(
                        new ActionSequence { Id = inputId, Type = "key" },
                        new ActionItem { Type = "keyDown", Value = "Control" }
                    ));
                    source.Ctrl = true;
                    actions.Add(new Action(
                        new ActionSequence { Id = inputId, Type = "key" },
                        new ActionItem { Type = "keyDown", Value = "Alt" }
                    ));
                    source.Alt = true;
                }

                // Add the individual key's down/up pair.
                actions.Add(new Action(
                    new ActionSequence { Id = inputId, Type = "key" },
                    new ActionItem { Type = "keyDown", Value = c.ToString() }
                ));
                actions.Add(new Action(
                    new ActionSequence { Id = inputId, Type = "key" },
                    new ActionItem { Type = "keyUp", Value = c.ToString() }
                ));

                // If we injected AltGr for this character, release them immediately.
                if (needsAltGr)
                {
                    actions.Add(new Action(
                        new ActionSequence { Id = inputId, Type = "key" },
                        new ActionItem { Type = "keyUp", Value = "Alt" }
                    ));
                    source.Alt = false;
                    actions.Add(new Action(
                        new ActionSequence { Id = inputId, Type = "key" },
                        new ActionItem { Type = "keyUp", Value = "Control" }
                    ));
                    source.Ctrl = false;
                }
            }

            // If Shift was pressed for this segment, release it after processing.
            if (needsShift)
            {
                actions.Add(new Action(
                    new ActionSequence { Id = inputId, Type = "key" },
                    new ActionItem { Type = "keyUp", Value = Keys.LeftShift }
                ));
                source.Shift = false;
            }
        }

        // ----- Remaining dispatch methods (DispatchKeyAction, DispatchPointerAction, etc.) -----

        private static async Task DispatchNullAction(Session session, Action action)
        {
            if (action.SubType == "pause")
                await Task.Yield();
            else
                throw WebDriverResponseException.InvalidArgument($"Null action subtype {action.SubType} unknown");
        }

        private async Task DispatchKeyAction(Session session, Action action)
        {
            if (action.Value == null)
                return;
            var source = session.InputState.GetInputSource<KeyInputSource>(action.Id)
                ?? throw WebDriverResponseException.UnknownError($"Input source for key action '{action.Id}' not found.");

            // For non-PACKET keys we process keyDown and keyUp normally.
            // For PACKET (Unicode) keys, we send the full keystroke only on keyDown and then ignore keyUp.
            switch (action.SubType)
            {
                case "keyDown":
                    {
                        var key = Keys.GetNormalizedKeyValue(action.Value);
                        var virtualKey = Keys.GetVirtualKey(key);
                        if (key == "Alt") source.Alt = true;
                        else if (key == "Shift") source.Shift = true;
                        else if (key == "Control") source.Ctrl = true;
                        else if (key == "Meta") source.Meta = true;
                        source.Pressed.Add(action.Value);
                        if (virtualKey == VirtualKeyShort.PACKET)
                        {
                            // For Unicode injection, send the complete keystroke.
                            Keys.DispatchUnicodeKeystroke(key[0]);
                            // Mark the key as "handled" so that the subsequent keyUp is ignored.
                            source.MarkUnicodeKeyHandled(key);
                        }
                        else
                        {
                            Keyboard.Press(virtualKey);
                        }
                        break;
                    }
                case "keyUp":
                    {
                        var key = Keys.GetNormalizedKeyValue(action.Value);
                        var virtualKey = Keys.GetVirtualKey(key);
                        if (key == "Alt") source.Alt = false;
                        else if (key == "Shift") source.Shift = false;
                        else if (key == "Control") source.Ctrl = false;
                        else if (key == "Meta") source.Meta = false;
                        source.Pressed.Remove(action.Value);
                        // If this Unicode key down already handled the keystroke, skip sending key up.
                        if (virtualKey == VirtualKeyShort.PACKET && source.IsUnicodeKeyHandled(key))
                        {
                            source.UnmarkUnicodeKeyHandled(key);
                        }
                        else
                        {
                            Keyboard.Release(virtualKey);
                        }
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
                    //_logger.LogDebug("Dispatching wheel scroll action, coordinates ({X},{Y}), delta ({DeltaX},{DeltaY}) with ID '{Id}'",
                    //    action.X, action.Y, action.DeltaX, action.DeltaY, action.Id);
                    if (action.X == null || action.Y == null)
                        throw WebDriverResponseException.InvalidArgument("For wheel scroll, X and Y are required");
                    Mouse.MoveTo(action.X.Value, action.Y.Value);
                    if (action.DeltaY.GetValueOrDefault() != 0)
                        Mouse.Scroll(action.DeltaY.GetValueOrDefault());
                    if (action.DeltaX.GetValueOrDefault() != 0)
                        Mouse.HorizontalScroll(action.DeltaX.GetValueOrDefault());
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
                    //_logger.LogDebug("Dispatching pointer move action, coordinates ({X},{Y}) with origin {Origin}, with ID '{Id}'",
                    //    action.X, action.Y, action.Origin, action.Id);
                    var point = GetCoordinates(session, action);
                    Mouse.MoveTo(point);
                    break;
                case "pointerDown":
                    //_logger.LogDebug("Dispatching pointer down action, button {Button}, with ID '{Id}'", action.Button, action.Id);
                    Mouse.Down(GetMouseButton(action.Button));
                    var cancelAction = action.Clone();
                    cancelAction.SubType = "pointerUp";
                    session.InputState.InputCancelList.Add(cancelAction);
                    break;
                case "pointerUp":
                    //_logger.LogDebug("Dispatching pointer up action, button {Button}, with ID '{Id}'", action.Button, action.Id);
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
