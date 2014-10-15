using System;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Cache.Session
{
    [AssemblyNeutral]
    public interface ISession // TODO: IDisposable
    {
        ISessionCollection Collection { get; }
        void Load();
        void Commit();
        bool TryCommitIfNotModifiedElsewhere();
    }
}