using System.Collections.Concurrent;

namespace FlaUI.WebDriver
{
    public class SessionRepository : ISessionRepository
    {
        // Thread-safe session storage
        private readonly ConcurrentDictionary<string, Session> _sessions 
            = new ConcurrentDictionary<string, Session>();

        private List<Session> Sessions { get; } = new List<Session>();

        public Session? FindById(string sessionId)
        {
            _sessions.TryGetValue(sessionId, out var session);
            return session;
        }
        public void Add(Session session) => _sessions[session.SessionId] = session;
        public void Delete(Session session) => _sessions.TryRemove(session.SessionId, out _);
        public List<Session> FindTimedOut() =>
            _sessions.Values.Where(s => s.IsTimedOut).ToList();

        public List<Session> FindAll()
        {
            return new List<Session>(Sessions);
        }
    }
}
