using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Builder;
using Autofac.Core;

namespace Microsoft.AspNet.DependencyInjection.Autofac
{
    public static class AutofacRegistration
    {
        public static void Populate(
                ContainerBuilder builder,
                IEnumerable<IServiceDescriptor> descriptors,
                params IEnumerable<IServiceDescriptor>[] moreDescriptors)
        {
            Populate(builder, null, descriptors, moreDescriptors);
        }

        public static void Populate(
                ContainerBuilder builder,
                IServiceProvider fallbackServiceProvider,
                IEnumerable<IServiceDescriptor> descriptors,
                params IEnumerable<IServiceDescriptor>[] moreDescriptors)
        {
            if (fallbackServiceProvider != null)
            {
                builder.RegisterSource(new ChainedRegistrationSource(fallbackServiceProvider));
            }

            builder.RegisterType<AutofacServiceProvider>().As<IServiceProvider>();
            builder.RegisterType<AutofacServiceScopeFactory>().As<IServiceScopeFactory>();

            Register(builder, descriptors);

            foreach (var serviceDescriptors in moreDescriptors)
            {
                Register(builder, serviceDescriptors);
            }
        }

        private static void Register(
                ContainerBuilder builder,
                IEnumerable<IServiceDescriptor> descriptors)
        {
            foreach (var descriptor in descriptors)
            {
                if (descriptor.ImplementationType != null)
                {
                    // Test if the an open generic type is being registered
                    var serviceTypeInfo = descriptor.ServiceType.GetTypeInfo();
                    if (serviceTypeInfo.IsGenericTypeDefinition)
                    {
                        builder
                            .RegisterGeneric(descriptor.ImplementationType)
                            .As(descriptor.ServiceType)
                            .ConfigureLifecycle(descriptor.Lifecycle);
                    }
                    else
                    {
                        builder
                            .RegisterType(descriptor.ImplementationType)
                            .As(descriptor.ServiceType)
                            .ConfigureLifecycle(descriptor.Lifecycle);
                    }
                }
                else
                {
                    builder
                        .RegisterInstance(descriptor.ImplementationInstance)
                        .As(descriptor.ServiceType)
                        .ConfigureLifecycle(descriptor.Lifecycle);
                }
            }
        }

        private static IRegistrationBuilder<object, T, U> ConfigureLifecycle<T, U>(
                this IRegistrationBuilder<object, T, U> registrationBuilder,
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

        private class AutofacServiceScopeFactory : IServiceScopeFactory
        {
            private readonly ILifetimeScope _lifetimeScope;

            public AutofacServiceScopeFactory(ILifetimeScope lifetimeScope)
            {
                _lifetimeScope = lifetimeScope;
            }

            public IServiceScope CreateScope()
            {
                return new AutofacServiceScope(_lifetimeScope.BeginLifetimeScope());
            }
        }

        private class AutofacServiceScope : IServiceScope
        {
            private readonly ILifetimeScope _lifetimeScope;
            private readonly IServiceProvider _serviceProvider;

            public AutofacServiceScope(ILifetimeScope lifetimeScope)
            {
                _lifetimeScope = lifetimeScope;
                _serviceProvider = _lifetimeScope.Resolve<IServiceProvider>();
            }

            public IServiceProvider ServiceProvider
            {
                get { return _serviceProvider; }
            }

            public void Dispose()
            {
                _lifetimeScope.Dispose();
            }
        }

        private class ChainedRegistrationSource : IRegistrationSource
        {
            private readonly IServiceProvider _fallbackServiceProvider;

            public ChainedRegistrationSource(IServiceProvider fallbackServiceProvider)
            {
                _fallbackServiceProvider = fallbackServiceProvider;
            }

            public bool IsAdapterForIndividualComponents
            {
                get { return false; }
            }

            public IEnumerable<IComponentRegistration> RegistrationsFor(
                    Service service,
                    Func<Service, IEnumerable<IComponentRegistration>> registrationAcessor)
            {
                var serviceWithType = service as IServiceWithType;

                // Only introduce services that are not already registered
                if (serviceWithType != null && !registrationAcessor(service).Any())
                {
                    var serviceType = serviceWithType.ServiceType;
                    if (_fallbackServiceProvider.HasService(serviceType))
                    {
                        yield return RegistrationBuilder.ForDelegate(serviceType, (c, p) =>
                        {
                            return _fallbackServiceProvider.GetService(serviceType);
                        })
                        .PreserveExistingDefaults()
                        .CreateRegistration();
                    }
                }
            }
        }
    }
}
