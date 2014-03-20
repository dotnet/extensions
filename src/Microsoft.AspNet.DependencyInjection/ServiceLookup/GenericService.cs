using System;
using System.Reflection;

namespace Microsoft.AspNet.DependencyInjection.ServiceLookup
{
    internal class GenericService : IGenericService
    {
        private readonly IServiceDescriptor _descriptor;

        public GenericService(IServiceDescriptor descriptor)
        {
            _descriptor = descriptor;
        }

        public LifecycleKind Lifecycle
        {
            get { return _descriptor.Lifecycle; }
        }

        public IService GetService(Type closedServiceType)
        {
            Type[] genericArguments = closedServiceType.GetTypeInfo().GenericTypeArguments;
            Type closedImplementationType =
                _descriptor.ImplementationType.MakeGenericType(genericArguments);

            var closedServiceDescriptor = new ServiceDescriptor
            {
                ServiceType = closedServiceType,
                ImplementationType = closedImplementationType,
                Lifecycle = Lifecycle
            };

            return new Service(closedServiceDescriptor);
        }
    }
}
