using System;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Cache.Session
{
    // TODO: Rename to ISessionStore & let Load take a key? This would allow the instance to be re-used across requests.
    // The key could be stored on the ISesssionFeature
    [AssemblyNeutral]
    public interface ISessionFactory
    {
        ISession Create();
        bool IsAvailable { get; }
    }
}