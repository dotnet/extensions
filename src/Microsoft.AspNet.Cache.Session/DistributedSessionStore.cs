using System;
using Microsoft.Framework.Cache.Distributed;

namespace Microsoft.AspNet.Cache.Session
{
    public class DistributedSessionStore : ISessionStore
    {
        private readonly IDistributedCache _cache;

        public DistributedSessionStore([NotNull] IDistributedCache cache)
        {
            _cache = cache;
        }

        public bool IsAvailable
        {
            get
            {
                return true; // TODO:
            }
        }

        public void Connect()
        {
            _cache.Connect();
        }

        public ISession Create(string sessionId, TimeSpan idleTimeout)
        {
            return new DistributedSession(_cache, sessionId, idleTimeout);
        }
    }
}