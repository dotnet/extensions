using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.AspNet.DependencyInjection.MultiServiceFactories
{
    internal class MultiServiceFactory : IMultiServiceFactory
    {
        private readonly ServiceProvider _provider;
        private readonly IServiceDescriptor[] _descriptors;
        private readonly Func<object>[] _factories;

        public MultiServiceFactory(ServiceProvider provider, IServiceDescriptor[] descriptors)
        {
            Debug.Assert(provider != null);
            Debug.Assert(descriptors.Length > 0);
            Debug.Assert(descriptors.All(d => d.ServiceType == descriptors[0].ServiceType));

            _provider = provider;
            _descriptors = descriptors;
            _factories = new Func<object>[descriptors.Length];
            CreateSingletonFactories();
            CreateNonSingletonFactories();
        }

        // Copy constructor
        private MultiServiceFactory(
                ServiceProvider provider,
                IServiceDescriptor[] descriptors,
                Func<object>[] factories)
        {
            _provider = provider;
            _descriptors = descriptors;
            _factories = factories;
        }

        public IMultiServiceFactory Scope(ServiceProvider scopedProvider)
        {
            // Shallow-copy the _factories array since we replace the non-singletons
            var factories = _factories.ToArray();
            var scope = new MultiServiceFactory(scopedProvider, _descriptors, factories);

            // Regenerate all factories that are not singletons
            scope.CreateNonSingletonFactories();

            return scope;
        }

        public object GetSingleService()
        {
            return _factories[0]();
        }

        public IList GetMultiService()
        {
            Type serviceType = _descriptors[0].ServiceType;
            Type listType = typeof(List<>).MakeGenericType(serviceType);
            var services = (IList)Activator.CreateInstance(listType);

            foreach (var factory in _factories)
            {
                services.Add(factory());
            }

            return services;
        }

        // CreateSingletonFactories is only called for the top-level container
        // These top-level factories get copied by MultiServiceFactory.Scope 
        private void CreateSingletonFactories()
        {
            for (int i = 0; i < _descriptors.Length; i++)
            {
                if (_descriptors[i].Lifecycle == LifecycleKind.Singleton)
                {
                    _factories[i] = CreateNonTransientServiceFactory(_descriptors[i]);
                }
            }
        }

        private void CreateNonSingletonFactories()
        {
            for (int i = 0; i < _descriptors.Length; i++)
            {
                if (_descriptors[i].Lifecycle == LifecycleKind.Scoped)
                {
                    _factories[i] = CreateNonTransientServiceFactory(_descriptors[i]);
                }
                else if (_descriptors[i].Lifecycle == LifecycleKind.Transient)
                {
                    _factories[i] = CreateTransientServiceFactory(_descriptors[i]);
                }
            }
        }

        private Func<object> CreateNonTransientServiceFactory(IServiceDescriptor descriptor)
        {
            Debug.Assert(descriptor.Lifecycle != LifecycleKind.Transient);

            if (descriptor.ImplementationType != null)
            {
                Func<IServiceProvider, object> serviceFactory =
                    ActivatorUtilities.CreateFactory(descriptor.ImplementationType);

                var singletonServiceFactory = new Lazy<object>(
                    () => _provider.CaptureDisposableService(serviceFactory(_provider)));
                return () => singletonServiceFactory.Value;
            }
            else
            {
                Debug.Assert(descriptor.ImplementationInstance != null);
                Debug.Assert(descriptor.Lifecycle == LifecycleKind.Singleton);

                _provider.CaptureDisposableService(descriptor.ImplementationInstance);
                return () => descriptor.ImplementationInstance;
            }
        }

        private Func<object> CreateTransientServiceFactory(IServiceDescriptor descriptor)
        {
            Debug.Assert(descriptor.Lifecycle == LifecycleKind.Transient);
            Debug.Assert(descriptor.ImplementationType != null);

            Func<IServiceProvider, object> serviceFactory =
                ActivatorUtilities.CreateFactory(descriptor.ImplementationType);

            return () => _provider.CaptureDisposableService(serviceFactory(_provider));
        }
    }
}
