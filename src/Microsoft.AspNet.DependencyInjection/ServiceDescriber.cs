using System;
using Microsoft.AspNet.ConfigurationModel;

namespace Microsoft.AspNet.DependencyInjection
{
    public class ServiceDescriber
    {
        private IConfiguration _configuration;

        public ServiceDescriber()
            : this(new Configuration())
        {
        }

        public ServiceDescriber(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public ServiceDescriptor Transient<TService, TImplementation>()
        {
            return Describe<TService, TImplementation>(LifecycleKind.Transient);
        }

        public ServiceDescriptor Scoped<TService, TImplementation>()
        {
            return Describe<TService, TImplementation>(LifecycleKind.Scoped);
        }

        public ServiceDescriptor Singleton<TService, TImplementation>()
        {
            return Describe<TService, TImplementation>(LifecycleKind.Singleton);
        }

        public ServiceDescriptor Instance<TService>(object implementationInstance)
        {
            return Describe(
                typeof(TService),
                null, // implementationType
                implementationInstance,
                LifecycleKind.Singleton);
        }

        private ServiceDescriptor Describe<TService, TImplementation>(LifecycleKind lifecycle)
        {
            return Describe(
                typeof(TService),
                typeof(TImplementation),
                null, // implementationInstance
                lifecycle);
        }

        public ServiceDescriptor Describe(
                Type serviceType,
                Type implementationType,
                object implementationInstance,
                LifecycleKind lifecycle)
        {
            var serviceTypeName = serviceType.FullName;
            var implementationTypeName = _configuration.Get(serviceTypeName);
            if (!String.IsNullOrEmpty(implementationTypeName))
            {
                try
                {
                    implementationType = Type.GetType(implementationTypeName);
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("TODO: unable to locate implementation {0} for service {1}", implementationTypeName, serviceTypeName), ex);
                }
            }

            return new ServiceDescriptor
            {
                ServiceType = serviceType,
                ImplementationType = implementationType,
                ImplementationInstance = implementationInstance,
                Lifecycle = lifecycle
            };
        }
    }
}
