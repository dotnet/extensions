using System;
using System.Collections.Generic;
using Autofac;

namespace Microsoft.AspNet.DependencyInjection.Autofac
{
    public static class AutofacRegistration
    {
        public static void Populate(
                ContainerBuilder builder,
                IEnumerable<IServiceDescriptor> firstServiceDescriptors,
                params IEnumerable<IServiceDescriptor>[] moreServiceDescriptors)
        {
            builder.RegisterType<AutofacServiceProvider>().As<IServiceProvider>();

            Register(builder, firstServiceDescriptors);

            foreach (var serviceDescriptors in moreServiceDescriptors)
            {
                Register(builder, serviceDescriptors);
            }
        }

        private static void Register(
                ContainerBuilder builder,
                IEnumerable<IServiceDescriptor> serviceDescriptors)
        {
            foreach (var serviceDescriptor in serviceDescriptors)
            {
                if (serviceDescriptor.ImplementationType != null)
                {
                    builder
                        .RegisterType(serviceDescriptor.ImplementationType)
                        .As(serviceDescriptor.ServiceType);
                }
                else
                {
                    builder
                        .RegisterInstance(serviceDescriptor.ImplementationInstance)
                        .As(serviceDescriptor.ServiceType);
                }
            }
        }

        private class AutofacServiceProvider : IServiceProvider
        {
            private readonly IComponentContext _componentContext;

            public AutofacServiceProvider(IComponentContext componentContext)
            {
                _componentContext = componentContext;
            }

            public object GetService(Type serviceType)
            {
                return _componentContext.Resolve(serviceType);
            }
        }
    }
}
