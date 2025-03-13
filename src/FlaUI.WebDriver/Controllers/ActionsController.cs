using System.Linq;
using System.Threading.Tasks;
using FlaUI.WebDriver.Models;
using FlaUI.WebDriver.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FlaUI.WebDriver.Controllers
{
    [Route("session/{sessionId}/[controller]")]
    [ApiController]
    public class ActionsController : ControllerBase
    {
        private readonly ILogger<ActionsController> _logger;
        private readonly ISessionRepository _sessionRepository;
        private readonly IActionsDispatcher _actionsDispatcher;

        public ActionsController(ILogger<ActionsController> logger, ISessionRepository sessionRepository, IActionsDispatcher actionsDispatcher)
        {
            _logger = logger;
            _sessionRepository = sessionRepository;
            _actionsDispatcher = actionsDispatcher;
        }

        [HttpPost]
        public ActionResult PerformActions([FromRoute] string sessionId, [FromBody] ActionsRequest actionsRequest)
        {
            _logger.LogDebug("Performing actions for session {SessionId}", sessionId);
            var session = GetSession(sessionId);

            // If the action sequence is for keys: process them in one batch synchronously.
            if (actionsRequest.Actions.Count == 1 && actionsRequest.Actions[0].Type == "key")
            {
                var actionSequence = actionsRequest.Actions[0];
                var fullText = string.Concat(actionSequence.Actions.Select(a => a.Value));

                // Get or create the input source and ensure it's a KeyInputSource.
                var inputSourceObj = session.InputState.GetOrCreateInputSource("key", actionSequence.Id);
                if (!(inputSourceObj is KeyInputSource keySource))
                {
                    throw new InvalidOperationException("Input source is not a valid KeyInputSource.");
                }

                _actionsDispatcher.DispatchActionsForStringSync(session, actionSequence.Id, keySource, fullText);
            }
            else
            {
                // Existing fallback for actions per tick asynchronously...
                var actionsByTick = ExtractActionSequence(session, actionsRequest);
                foreach (var tickActions in actionsByTick)
                {
                    var tickDuration = tickActions.Max(tickAction => tickAction.Duration) ?? 0;
                    var dispatchTickActionTasks = tickActions.Select(tickAction => _actionsDispatcher.DispatchAction(session, tickAction));
                    if (tickDuration > 0)
                    {
                        dispatchTickActionTasks = dispatchTickActionTasks.Concat(new[] { Task.Delay(tickDuration) });
                    }
                    Task.WhenAll(dispatchTickActionTasks).GetAwaiter().GetResult();
                }
            }
            return WebDriverResult.Success();
        }

        [HttpDelete]
        public async Task<ActionResult> ReleaseActions([FromRoute] string sessionId)
        {
            _logger.LogDebug("Releasing actions for session {SessionId}", sessionId);
            var session = GetSession(sessionId);
            foreach (var cancelAction in session.InputState.InputCancelList)
            {
                await _actionsDispatcher.DispatchAction(session, cancelAction);
            }
            session.InputState.Reset();
            return WebDriverResult.Success();
        }

        private static System.Collections.Generic.List<System.Collections.Generic.List<Action>> ExtractActionSequence(Session session, ActionsRequest actionsRequest)
        {
            var actionsByTick = new System.Collections.Generic.List<System.Collections.Generic.List<Action>>();
            foreach (var actionSequence in actionsRequest.Actions)
            {
                if (actionSequence.Type == "key")
                {
                    session.InputState.GetOrCreateInputSource(actionSequence.Type, actionSequence.Id);
                }
                for (var tickIndex = 0; tickIndex < actionSequence.Actions.Count; tickIndex++)
                {
                    var actionItem = actionSequence.Actions[tickIndex];
                    var action = new Action(actionSequence, actionItem);
                    if (actionsByTick.Count < tickIndex + 1)
                    {
                        actionsByTick.Add(new System.Collections.Generic.List<Action>());
                    }
                    actionsByTick[tickIndex].Add(action);
                }
            }
            return actionsByTick;
        }

        private Session GetSession(string sessionId)
        {
            var session = _sessionRepository.FindById(sessionId);
            if (session == null)
            {
                throw WebDriverResponseException.SessionNotFound(sessionId);
            }
            session.SetLastCommandTimeToNow();
            return session;
        }
    }
}
