// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal abstract class ServiceProviderEngine : IServiceProviderEngine, IServiceScopeFactory
    {
        private readonly IServiceProviderEngineCallback _callback;

        private readonly Func<Type, Func<ServiceProviderEngineScope, object>> _createServiceAccessor;

        protected ServiceProviderEngine(IEnumerable<ServiceDescriptor> serviceDescriptors, IServiceProviderEngineCallback callback)
        {
            _createServiceAccessor = CreateServiceAccessor;
            _callback = callback;

            Root = new ServiceProviderEngineScope(this);
            RuntimeResolver = new CallSiteRuntimeResolver();
            ExpressionBuilder = new CallSiteExpressionBuilder(RuntimeResolver, this, Root);
            CallSiteFactory = new CallSiteFactory(serviceDescriptors);
            CallSiteFactory.Add(typeof(IServiceProvider), new ServiceProviderCallSite());
            CallSiteFactory.Add(typeof(IServiceScopeFactory), new ServiceScopeFactoryCallSite());
        }

        internal ConcurrentDictionary<Type, Func<ServiceProviderEngineScope, object>> RealizedServices { get; } =
            new ConcurrentDictionary<Type, Func<ServiceProviderEngineScope, object>>();

        internal CallSiteFactory CallSiteFactory { get; }

        protected CallSiteRuntimeResolver RuntimeResolver { get; }

        protected CallSiteExpressionBuilder ExpressionBuilder { get; }

        public ServiceProviderEngineScope Root { get; }

        public IServiceScope RootScope => Root;

        public object GetService(Type serviceType) => GetService(serviceType, Root);

        protected abstract Func<ServiceProviderEngineScope, object> RealizeService(IServiceCallSite callSite);

        public void Dispose() => Root.Dispose();

        private Func<ServiceProviderEngineScope, object> CreateServiceAccessor(Type serviceType)
        {
            var callSite = CallSiteFactory.CreateCallSite(serviceType, new HashSet<Type>());
            if (callSite != null)
            {
                _callback?.OnCreate(callSite);
                return RealizeService(callSite);
            }

            return _ => null;
        }

        internal object GetService(Type serviceType, ServiceProviderEngineScope serviceProviderEngineScope)
        {
            var realizedService = RealizedServices.GetOrAdd(serviceType, _createServiceAccessor);
            _callback?.OnResolve(serviceType, serviceProviderEngineScope);
            return realizedService.Invoke(serviceProviderEngineScope);
        }

        public IServiceScope CreateScope()
        {
            return new ServiceProviderEngineScope(this);
        }
    }
}