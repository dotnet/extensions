using System;

namespace Microsoft.AspNet.DependencyInjection.ServiceLookup
{
    internal interface IGenericService
    {
        LifecycleKind Lifecycle { get; }

        IService GetService(Type closedServiceType);
    }
}
