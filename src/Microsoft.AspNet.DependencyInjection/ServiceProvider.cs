using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.AspNet.DependencyInjection.MultiServiceFactories;

namespace Microsoft.AspNet.DependencyInjection
{
    /// <summary>
    /// The default IServiceProvider.
    /// </summary>
    internal class ServiceProvider : IServiceProvider, IDisposable
    {
        private readonly IServiceProvider _fallbackServiceProvider;
        private readonly IDictionary<Type, IMultiServiceFactory> _factories;

        private ConcurrentBag<IDisposable> _disposables = new ConcurrentBag<IDisposable>();

        public ServiceProvider(IEnumerable<IServiceDescriptor> serviceDescriptors)
            : this(serviceDescriptors, fallbackServiceProvider: null)
        {
        }

        public ServiceProvider(
                IEnumerable<IServiceDescriptor> serviceDescriptors,
                IServiceProvider fallbackServiceProvider)
        {
            _fallbackServiceProvider = fallbackServiceProvider;

            var groupedDescriptors = serviceDescriptors.GroupBy(descriptor => descriptor.ServiceType);
            _factories = groupedDescriptors.ToDictionary(
                grouping => grouping.Key,
                grouping => (IMultiServiceFactory)new MultiServiceFactory(this, grouping.ToArray()));

            _factories[typeof(IServiceProvider)] = new ServiceProviderFactory(this);
            _factories[typeof(IServiceScopeFactory)] = new ServiceScopeFactoryFactory(this);
        }

        // This constructor is called exclusively to create a child scope from the parent
        internal ServiceProvider(ServiceProvider parent)
        {
            // Rescope the fallback service provider if it contains an IServiceScopeFactory
            var scopeFactory = GetFallbackServiceOrNull<IServiceScopeFactory>();
            if (scopeFactory != null)
            {
                var scope = scopeFactory.CreateScope();
                _fallbackServiceProvider = scope.ServiceProvider;
                _disposables.Add(scope);
            }
            else
            {
                _fallbackServiceProvider = parent._fallbackServiceProvider;
            }

            // Rescope all the factories
            _factories = parent._factories.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Scope(this));
        }

        /// <summary>
        /// Gets the service object of the specified type.
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public virtual object GetService(Type serviceType)
        {
            return GetSingleService(serviceType) ??
                GetMultiService(serviceType) ??
                GetFallbackService(serviceType);
        }

        private object GetSingleService(Type serviceType)
        {
            IMultiServiceFactory serviceFactory;
            return _factories.TryGetValue(serviceType, out serviceFactory)
                ? serviceFactory.GetSingleService()
                : null;
        }

        private IList GetMultiService(Type collectionType)
        {
            if (collectionType.GetTypeInfo().IsGenericType &&
                collectionType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                Type serviceType = collectionType.GetTypeInfo().GenericTypeArguments.Single();

                IMultiServiceFactory serviceFactory;
                if (_factories.TryGetValue(serviceType, out serviceFactory))
                {
                    return serviceFactory.GetMultiService();
                }
            }

            return null;
        }

        private object GetFallbackService(Type serviceType)
        {
            return _fallbackServiceProvider != null ? _fallbackServiceProvider.GetService(serviceType) : null;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "IServiceProvider may throw unknown exceptions")]
        private T GetFallbackServiceOrNull<T>() where T : class
        {
            try
            {
                return (T)GetFallbackService(typeof(T));
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            var disposables = Interlocked.Exchange(ref _disposables, null);

            if (disposables != null)
            {
                foreach (var disposable in disposables)
                {
                    disposable.Dispose();
                }
            }
        }

        internal object CaptureDisposableService(object service)
        {
            IDisposable disposable = service as IDisposable;
            if (disposable != null)
            {
                _disposables.Add(disposable);
            }

            return service;
        }
    }
}
