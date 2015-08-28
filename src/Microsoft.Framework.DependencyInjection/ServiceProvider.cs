// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Framework.DependencyInjection.ServiceLookup;

namespace Microsoft.Framework.DependencyInjection
{
    /// <summary>
    /// The default IServiceProvider.
    /// </summary>
    internal class ServiceProvider : IServiceProvider, IDisposable
    {
        private readonly object _sync = new object();

        private readonly Func<Type, Func<ServiceProvider, object>> _createServiceAccessor;
        private readonly ServiceProvider _root;
        private readonly ServiceTable _table;

        private readonly Dictionary<IService, object> _resolvedServices = new Dictionary<IService, object>();
        private ConcurrentBag<IDisposable> _disposables = new ConcurrentBag<IDisposable>();

        public ServiceProvider(IEnumerable<ServiceDescriptor> serviceDescriptors)
        {
            _root = this;
            _table = new ServiceTable(serviceDescriptors);

            _table.Add(typeof(IServiceProvider), new ServiceProviderService());
            _table.Add(typeof(IServiceScopeFactory), new ServiceScopeService());
            _table.Add(typeof(IEnumerable<>), new OpenIEnumerableService(_table));

            // Caching to avoid allocating a Func<,> object per call to GetService
            _createServiceAccessor = CreateServiceAccessor;
        }

        // This constructor is called exclusively to create a child scope from the parent
        internal ServiceProvider(ServiceProvider parent)
        {
            _root = parent._root;
            _table = parent._table;

            // Caching to avoid allocating a Func<,> object per call to GetService
            _createServiceAccessor = CreateServiceAccessor;
        }

        /// <summary>
        /// Gets the service object of the specified type.
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public object GetService(Type serviceType)
        {
            var realizedService = _table.RealizedServices.GetOrAdd(serviceType, _createServiceAccessor);
            return realizedService.Invoke(this);
        }

        private Func<ServiceProvider, object> CreateServiceAccessor(Type serviceType)
        {
            var callSite = GetServiceCallSite(serviceType, new HashSet<Type>());
            if (callSite != null)
            {
                return RealizeService(_table, serviceType, callSite);
            }

            return _ => null;
        }

        internal static Func<ServiceProvider, object> RealizeService(ServiceTable table, Type serviceType, IServiceCallSite callSite)
        {
            var callCount = 0;
            return provider =>
            {
                if (Interlocked.Increment(ref callCount) == 2)
                {
                    Task.Run(() =>
                    {
                        var providerExpression = Expression.Parameter(typeof(ServiceProvider), "provider");

                        var lambdaExpression = Expression.Lambda<Func<ServiceProvider, object>>(
                            callSite.Build(providerExpression),
                            providerExpression);

                        table.RealizedServices[serviceType] = lambdaExpression.Compile();
                    });
                }

                return callSite.Invoke(provider);
            };
        }

        internal IServiceCallSite GetServiceCallSite(Type serviceType, ISet<Type> callSiteChain)
        {
            try
            {
                if (callSiteChain.Contains(serviceType))
                {
                    throw new InvalidOperationException(Resources.FormatCircularDependencyException(serviceType));
                }

                callSiteChain.Add(serviceType);

                ServiceEntry entry;
                if (_table.TryGetEntry(serviceType, out entry))
                {
                    return GetResolveCallSite(entry.Last, callSiteChain);
                }

                object emptyIEnumerableOrNull = GetEmptyIEnumerableOrNull(serviceType);
                if (emptyIEnumerableOrNull != null)
                {
                    return new EmptyIEnumerableCallSite(serviceType, emptyIEnumerableOrNull);
                }

                return null;
            }
            finally
            {
                callSiteChain.Remove(serviceType);
            }

        }

        internal IServiceCallSite GetResolveCallSite(IService service, ISet<Type> callSiteChain)
        {
            IServiceCallSite serviceCallSite = service.CreateCallSite(this, callSiteChain);
            if (service.Lifetime == ServiceLifetime.Transient)
            {
                return new TransientCallSite(serviceCallSite);
            }
            else if (service.Lifetime == ServiceLifetime.Scoped)
            {
                return new ScopedCallSite(service, serviceCallSite);
            }
            else
            {
                return new SingletonCallSite(service, serviceCallSite);
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

        private static MethodInfo CaptureDisposableMethodInfo = GetMethodInfo<Func<ServiceProvider, object, object>>((a, b) => a.CaptureDisposable(b));
        private static MethodInfo TryGetValueMethodInfo = GetMethodInfo<Func<IDictionary<IService, object>, IService, object, bool>>((a, b, c) => a.TryGetValue(b, out c));
        private static MethodInfo AddMethodInfo = GetMethodInfo<Action<IDictionary<IService, object>, IService, object>>((a, b, c) => a.Add(b, c));

        private static MethodInfo MonitorEnterMethodInfo = GetMethodInfo<Action<object, bool>>((lockObj, lockTaken) => Monitor.Enter(lockObj, ref lockTaken));
        private static MethodInfo MonitorExitMethodInfo = GetMethodInfo<Action<object>>(lockObj => Monitor.Exit(lockObj));

        private static MethodInfo GetMethodInfo<T>(Expression<T> expr)
        {
            var mc = (MethodCallExpression)expr.Body;
            return mc.Method;
        }

        private class EmptyIEnumerableCallSite : IServiceCallSite
        {
            private readonly object _serviceInstance;
            private readonly Type _serviceType;

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
            private readonly IServiceCallSite _service;

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
                return Expression.Call(
                    provider,
                    CaptureDisposableMethodInfo,
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
                object resolved;
                lock (provider._sync)
                {
                    if (!provider._resolvedServices.TryGetValue(_key, out resolved))
                    {
                        resolved = provider.CaptureDisposable(_serviceCallSite.Invoke(provider));
                        provider._resolvedServices.Add(_key, resolved);
                    }
                }
                return resolved;
            }

            public virtual Expression Build(Expression providerExpression)
            {
                var keyExpression = Expression.Constant(
                    _key,
                    typeof(IService));

                var resolvedExpression = Expression.Variable(typeof(object), "resolved");

                var resolvedServicesExpression = Expression.Field(
                    providerExpression,
                    "_resolvedServices");

                var tryGetValueExpression = Expression.Call(
                    resolvedServicesExpression,
                    TryGetValueMethodInfo,
                    keyExpression,
                    resolvedExpression);

                var captureDisposableExpression = Expression.Assign(
                    resolvedExpression,
                    Expression.Call(
                        providerExpression,
                        CaptureDisposableMethodInfo,
                        _serviceCallSite.Build(providerExpression)));

                var addValueExpression = Expression.Call(
                    resolvedServicesExpression,
                    AddMethodInfo,
                    keyExpression,
                    resolvedExpression);

                var blockExpression = Expression.Block(
                    typeof(object),
                    new[] { resolvedExpression },
                    Expression.IfThen(
                        Expression.Not(tryGetValueExpression),
                        Expression.Block(captureDisposableExpression, addValueExpression)),
                    resolvedExpression);

                return Lock(providerExpression, blockExpression);
            }

            private static Expression Lock(Expression providerExpression, Expression body)
            {
                // The C# compiler would copy the lock object to guard against mutation.
                // We don't, since we know the lock object is readonly.
                var syncField = Expression.Field(providerExpression, "_sync");
                var lockWasTaken = Expression.Variable(typeof(bool), "lockWasTaken");

                var monitorEnter = Expression.Call(MonitorEnterMethodInfo, syncField, lockWasTaken);
                var monitorExit = Expression.Call(MonitorExitMethodInfo, syncField);

                var tryBody = Expression.Block(monitorEnter, body);
                var finallyBody = Expression.IfThen(lockWasTaken, monitorExit);

                return Expression.Block(
                    typeof(object),
                    new[] { lockWasTaken },
                    Expression.TryFinally(tryBody, finallyBody));
            }
        }

        private class SingletonCallSite : ScopedCallSite
        {
            public SingletonCallSite(IService key, IServiceCallSite serviceCallSite) : base(key, serviceCallSite)
            {
            }

            public override object Invoke(ServiceProvider provider)
            {
                return base.Invoke(provider._root);
            }

            public override Expression Build(Expression provider)
            {
                return base.Build(Expression.Field(provider, "_root"));
            }
        }
    }
}
