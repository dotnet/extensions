using System;

namespace Microsoft.AspNet.Cache.Session
{
    public class SessionFeature : ISessionFeature
    {
        public ISessionFactory Factory { get; set; }

        public ISession Session { get; set; }
    }
}