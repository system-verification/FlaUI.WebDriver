
namespace FlaUI.WebDriver.Services
{
    public interface IActionsDispatcher
    {
        Task DispatchAction(Session session, Action action);
        void DispatchActionsForStringSync(Session session, string inputId, KeyInputSource source, string text);
        Task DispatchActionsForString(Session session, string inputId, KeyInputSource source, string text);
        Task DispatchActionsForStringWithFlaUICore(Session session, string inputId, KeyInputSource source, string text);
    }
}
