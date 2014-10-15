using System;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Cache.Session
{
    // TODO: Is there any reason not to flatten the Factory down into the Feature?
    [AssemblyNeutral]
    public interface ISessionFeature
    {
        ISessionFactory Factory { get; set; }

        ISession Session { get; set; }
    }
}