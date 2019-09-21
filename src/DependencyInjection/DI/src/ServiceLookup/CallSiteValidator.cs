// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class CallSiteValidator: CallSiteVisitor<CallSiteValidator.CallSiteValidatorState, Type>
    {
        // Keys are services being resolved via GetService, values - first scoped service in their call site tree
        private readonly ConcurrentDictionary<Type, Type> _scopedServices = new ConcurrentDictionary<Type, Type>();

        // Cache already-checked services that resulted in null.
        private readonly HashSet<Type> _nonScopedServices = new HashSet<Type>();

        public void ValidateCallSite(ServiceCallSite callSite)
        {
            VisitCallSite(callSite, default);
        }

        protected override Type VisitCallSite(ServiceCallSite callSite, CallSiteValidatorState argument)
        {
            Type scoped;
            bool ignoreServiceType = argument.IgnoreServiceType;

            if ((!ignoreServiceType && _scopedServices.TryGetValue(callSite.ServiceType, out scoped)) ||
                (callSite.ImplementationType != null && _scopedServices.TryGetValue(callSite.ImplementationType, out scoped)))
            {
                return scoped;
            }
            else
            {
                lock (_nonScopedServices)
                {
                    if ((!ignoreServiceType && _nonScopedServices.Contains(callSite.ServiceType)) ||
                        (callSite.ImplementationType != null && _nonScopedServices.Contains(callSite.ImplementationType)))
                    {
                        return null;
                    }
                }
            }

            argument.IgnoreServiceType = false;

            scoped = base.VisitCallSite(callSite, argument);

            if (scoped != null)
            {
                if (!ignoreServiceType)
                {
                    _scopedServices[callSite.ServiceType] = scoped;
                }

                if (callSite.ImplementationType != null)
                {
                    _scopedServices[callSite.ImplementationType] = scoped;
                }
            }
            else
            {
                lock (_nonScopedServices)
                {
                    if (!ignoreServiceType)
                    {
                        _nonScopedServices.Add(callSite.ServiceType);
                    }

                    if (callSite.ImplementationType != null)
                    {
                        _nonScopedServices.Add(callSite.ImplementationType);
                    }
                }
            }

            return scoped;
        }

        public void ValidateResolution(Type serviceType, IServiceScope scope, IServiceScope rootScope)
        {
            if (ReferenceEquals(scope, rootScope)
                && _scopedServices.TryGetValue(serviceType, out var scopedService))
            {
                if (serviceType == scopedService)
                {
                    throw new InvalidOperationException(
                        Resources.FormatDirectScopedResolvedFromRootException(serviceType,
                            nameof(ServiceLifetime.Scoped).ToLowerInvariant()));
                }

                throw new InvalidOperationException(
                    Resources.FormatScopedResolvedFromRootException(
                        serviceType,
                        scopedService,
                        nameof(ServiceLifetime.Scoped).ToLowerInvariant()));
            }
        }

        protected override Type VisitConstructor(ConstructorCallSite constructorCallSite, CallSiteValidatorState state)
        {
            Type result = null;
            foreach (var parameterCallSite in constructorCallSite.ParameterCallSites)
            {
                var scoped =  VisitCallSite(parameterCallSite, state);
                if (result == null)
                {
                    result = scoped;
                }
            }
            return result;
        }

        protected override Type VisitIEnumerable(IEnumerableCallSite enumerableCallSite,
            CallSiteValidatorState state)
        {
            Type result = null;
            ServiceCallSite[] serviceCallSites = enumerableCallSite.ServiceCallSites;

            for (int i = 0; i < serviceCallSites.Length; i++)
            {
                // Ignore service type for all except the last element
                state.IgnoreServiceType = i != serviceCallSites.Length - 1;

                var scoped = VisitCallSite(serviceCallSites[i], state);
                if (result == null)
                {
                    result = scoped;
                }
            }
            return result;
        }

        protected override Type VisitRootCache(ServiceCallSite singletonCallSite, CallSiteValidatorState state)
        {
            state.Singleton = singletonCallSite;
            return VisitCallSiteMain(singletonCallSite, state);
        }

        protected override Type VisitScopeCache(ServiceCallSite scopedCallSite, CallSiteValidatorState state)
        {
            // We are fine with having ServiceScopeService requested by singletons
            if (scopedCallSite is ServiceScopeFactoryCallSite)
            {
                return null;
            }
            if (state.Singleton != null)
            {
                throw new InvalidOperationException(Resources.FormatScopedInSingletonException(
                    scopedCallSite.ServiceType,
                    state.Singleton.ServiceType,
                    nameof(ServiceLifetime.Scoped).ToLowerInvariant(),
                    nameof(ServiceLifetime.Singleton).ToLowerInvariant()
                    ));
            }

            VisitCallSiteMain(scopedCallSite, state);
            return scopedCallSite.ServiceType;
        }

        protected override Type VisitConstant(ConstantCallSite constantCallSite, CallSiteValidatorState state) => null;

        protected override Type VisitServiceProvider(ServiceProviderCallSite serviceProviderCallSite, CallSiteValidatorState state) => null;

        protected override Type VisitServiceScopeFactory(ServiceScopeFactoryCallSite serviceScopeFactoryCallSite, CallSiteValidatorState state) => null;

        protected override Type VisitFactory(FactoryCallSite factoryCallSite, CallSiteValidatorState state) => null;

        internal struct CallSiteValidatorState
        {
            public ServiceCallSite Singleton { get; set; }
            public bool IgnoreServiceType { get; set; }
        }
    }
}
