using System;
//using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.DependencyInjection
{
    //[AssemblyNeutral]
    public interface IServiceDescriptor
    {
        LifecycleKind Lifecycle { get; }
        Type ServiceType { get; }
        Type ImplementationType { get; } // nullable
        object ImplementationInstance { get; } // nullable
    }
}
