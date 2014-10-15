using System;

namespace Microsoft.AspNet.Cache.Session
{
    public interface ISessionStore
    {
        bool IsAvailable { get; }
        void Connect();
        ISession Create(string sessionId, TimeSpan idleTimeout);
    }
}