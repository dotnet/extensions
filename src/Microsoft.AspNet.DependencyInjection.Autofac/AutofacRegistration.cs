using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
                    builder
                        .RegisterType(descriptor.ImplementationType)
                        .As(descriptor.ServiceType)
                        .ConfigureLifecycle(descriptor.Lifecycle);
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
                    if (HasService(_fallbackServiceProvider, serviceType))
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

            private bool HasService(IServiceProvider provider, Type serviceType)
            {
                try
                {
                    var obj = provider.GetService(serviceType);

                    // Return false for empty enumerables
                    if(obj is IEnumerable)
                    {
                        return ((IEnumerable)obj).GetEnumerator().MoveNext();
                    }

                    return obj != null;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}
