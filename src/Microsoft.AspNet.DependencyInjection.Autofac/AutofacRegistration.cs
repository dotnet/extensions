using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Builder;

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
                        .As(serviceDescriptor.ServiceType)
                        .ConfigureLifecycle(serviceDescriptor.Lifecycle);
                }
                else
                {
                    builder
                        .RegisterInstance(serviceDescriptor.ImplementationInstance)
                        .As(serviceDescriptor.ServiceType)
                        .ConfigureLifecycle(serviceDescriptor.Lifecycle);
                }
            }
        }

        private static IRegistrationBuilder<object, T, SingleRegistrationStyle> ConfigureLifecycle<T>(
                this IRegistrationBuilder<object, T, SingleRegistrationStyle> registrationBuilder,
                LifecycleKind lifecycleKind)
        {
            switch (lifecycleKind)
            {
                case LifecycleKind.Singleton:
                    registrationBuilder.SingleInstance();
                    break;
                case LifecycleKind.Scoped:
                    registrationBuilder.InstancePerLifetimeScope();
                    break;
                case LifecycleKind.Transient:
                    registrationBuilder.InstancePerDependency();
                    break;
            }

            return registrationBuilder;
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
