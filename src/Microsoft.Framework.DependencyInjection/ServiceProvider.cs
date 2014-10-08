// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using Microsoft.Framework.DependencyInjection.ServiceLookup;

namespace Microsoft.Framework.DependencyInjection
{
    /// <summary>
    /// The default IServiceProvider.
    /// </summary>
    internal class ServiceProvider : IServiceProvider, IDisposable
    {
        private readonly object _sync = new object();

        private readonly ServiceProvider _parent;
        private readonly ServiceTable _table;
        private readonly IServiceProvider _fallback;

        private readonly Dictionary<IService, object> _resolvedServices = new Dictionary<IService, object>();
        private ConcurrentBag<IDisposable> _disposables = new ConcurrentBag<IDisposable>();

        public ServiceProvider Root
        {
            get
            {
                return _parent?.Root ?? this;
            }
        }

        public ServiceProvider(IEnumerable<IServiceDescriptor> serviceDescriptors)
            : this(serviceDescriptors, fallbackServiceProvider: null)
        {
        }

        public ServiceProvider(
                IEnumerable<IServiceDescriptor> serviceDescriptors,
                IServiceProvider fallbackServiceProvider)
        {
            _table = new ServiceTable(serviceDescriptors);
            _fallback = fallbackServiceProvider;

            _table.Add(typeof(IServiceProvider), new ServiceProviderService());
            _table.Add(typeof(IServiceScopeFactory), new ServiceScopeService());
            _table.Add(typeof(IEnumerable<>), new OpenIEnumerableService(_table));
        }

        // This constructor is called exclusively to create a child scope from the parent
        internal ServiceProvider(ServiceProvider parent)
        {
            _parent = parent;
            _table = parent._table;
            _fallback = parent._fallback;

            // Rescope the fallback service provider if it contains an IServiceScopeFactory
            var scopeFactory = GetFallbackServiceOrNull<IServiceScopeFactory>();
            if (scopeFactory != null)
            {
                var scope = scopeFactory.CreateScope();
                _fallback = scope.ServiceProvider;
                _disposables.Add(scope);
            }
        }

        /// <summary>
        /// Gets the service object of the specified type.
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public object GetService(Type serviceType)
        {
            var callSite = GetServiceCallSite(serviceType);
            if (callSite != null)
            {
                var providerExpression = Expression.Parameter(typeof(ServiceProvider), "provider");
                var serviceExpression = callSite.Build(providerExpression);
                var lambdaExpression = Expression.Lambda(
                    typeof(Func<ServiceProvider, object>),
                    serviceExpression,
                    providerExpression);

                var lambda = (Func<ServiceProvider, object>)lambdaExpression.Compile();
                return lambda(this);

//                return callSite.Invoke(this);
            }

            return null;
        }

        internal IServiceCallSite GetServiceCallSite(Type serviceType)
        {
            ServiceEntry entry;
            if (_table.TryGetEntry(serviceType, out entry))
            {
                return GetResolveCallSite(entry.Last);
            }

            object fallbackService = GetFallbackService(serviceType);
            if (fallbackService != null)
            {
                return new FallbackCallSite(serviceType);
            }

            object emptyIEnumerableOrNull = GetEmptyIEnumerableOrNull(serviceType);
            if (emptyIEnumerableOrNull != null)
            {
                return new EmptyIEnumerableCallSite(serviceType, emptyIEnumerableOrNull);
            }

            return null;
        }

        internal IServiceCallSite GetResolveCallSite(IService service)
        {
            IServiceCallSite serviceCallSite = service.CreateCallSite(this);
            if (service.Lifecycle == LifecycleKind.Transient)
            {
                return new TransientCallSite(serviceCallSite);
            }
            else if (service.Lifecycle == LifecycleKind.Scoped)
            {
                return new ScopedCallSite(service, serviceCallSite);
            }
            else
            {
                return new ScopedCallSite(service, serviceCallSite);
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

        private object GetFallbackService(Type serviceType)
        {
            return _fallback != null ? _fallback.GetService(serviceType) : null;
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

        private object CaptureDisposable(object service)
        {
            if (!object.ReferenceEquals(this, service))
            {
                var disposable = service as IDisposable;
                if (disposable != null)
                {
                    _disposables.Add(disposable);
                }
            }
            return service;
        }

        private object GetEmptyIEnumerableOrNull(Type serviceType)
        {
            var typeInfo = serviceType.GetTypeInfo();

            if (typeInfo.IsGenericType &&
                serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var itemType = typeInfo.GenericTypeArguments[0];
                return Array.CreateInstance(itemType, 0);
            }

            return null;
        }

        private class FallbackCallSite : IServiceCallSite
        {
            private Type _serviceType;

            public FallbackCallSite(Type serviceType)
            {
                _serviceType = serviceType;
            }

            public object Invoke(ServiceProvider provider)
            {
                return provider.GetFallbackService(_serviceType);
            }

            public Expression Build(Expression provider)
            {
                throw new NotImplementedException("FIX: make fast-path");
            }
        }

        private class EmptyIEnumerableCallSite : IServiceCallSite
        {
            private object _serviceInstance;
            private Type _serviceType;

            public EmptyIEnumerableCallSite(Type serviceType, object serviceInstance)
            {
                _serviceType = serviceType;
                _serviceInstance = serviceInstance;
            }

            public object Invoke(ServiceProvider provider)
            {
                return _serviceInstance;
            }

            public Expression Build(Expression provider)
            {
                return Expression.Constant(_serviceInstance, _serviceType);
            }
        }

        private class TransientCallSite : IServiceCallSite
        {
            private IServiceCallSite _service;

            public TransientCallSite(IServiceCallSite service)
            {
                _service = service;
            }

            public object Invoke(ServiceProvider provider)
            {
                return provider.CaptureDisposable(_service.Invoke(provider));
            }

            public Expression Build(Expression provider)
            {
                Expression<Func<object, object>> expr = _ => default(ServiceProvider).CaptureDisposable(_);
                var mc = (MethodCallExpression)expr.Body;
                var methodInfo = typeof(ServiceProvider).GetTypeInfo().GetMethod("CaptureDisposable");
                return Expression.Call(
                    provider,
                    mc.Method,
                    _service.Build(provider));
            }
        }

        private class ScopedCallSite : IServiceCallSite
        {
            private readonly IService _key;
            private readonly IServiceCallSite _serviceCallSite;

            public ScopedCallSite(IService key, IServiceCallSite serviceCallSite)
            {
                _key = key;
                _serviceCallSite = serviceCallSite;
            }

            public virtual object Invoke(ServiceProvider provider)
            {
                lock (provider._sync)
                {
                    object resolved;
                    if (!provider._resolvedServices.TryGetValue(_key, out resolved))
                    {
                        resolved = provider.CaptureDisposable(_serviceCallSite.Invoke(provider));
                        provider._resolvedServices[_key] = resolved;
                    }
                    return resolved;
                }
            }

            public virtual Expression Build(Expression provider)
            {
                throw new NotImplementedException();
            }
        }

        private class SingletonCallSite : ScopedCallSite
        {
            public SingletonCallSite(IService key, IServiceCallSite serviceCallSite) : base(key, serviceCallSite)
            {
            }

            public object Invoke(ServiceProvider provider)
            {
                return base.Invoke(provider.Root);
            }

            public Expression Build(Expression provider)
            {
                throw new NotImplementedException();
            }
        }
    }
}
