using System;
using System.Collections.Generic;
//using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.DependencyInjection
{
    //[AssemblyNeutral]
    public interface IServiceCollection : IEnumerable<IServiceDescriptor>
    {
        IServiceCollection Add(IServiceDescriptor descriptor);

        IServiceCollection Add(IEnumerable<IServiceDescriptor> descriptors);

        IServiceCollection AddTransient(Type service, Type implementationType);

        IServiceCollection AddScoped(Type service, Type implementationType);

        IServiceCollection AddSingleton(Type service, Type implementationType);

        IServiceCollection AddInstance(Type service, object implementationInstance);
    }
}
