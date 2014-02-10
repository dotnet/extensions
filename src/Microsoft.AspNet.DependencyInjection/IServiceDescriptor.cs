using System;

namespace Microsoft.AspNet.DependencyInjection
{
    public interface IServiceDescriptor
    {
        LifecycleKind Lifecycle { get; }
        Type ServiceType { get; }
        Type ImplementationType { get; } // nullable
        object ImplementationInstance { get; } // nullable
    }
}
