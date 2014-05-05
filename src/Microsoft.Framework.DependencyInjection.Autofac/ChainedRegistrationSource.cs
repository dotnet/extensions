// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Builder;
using Autofac.Core;

namespace Microsoft.Framework.DependencyInjection.Autofac
{
    internal class ChainedRegistrationSource : IRegistrationSource
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
                if (serviceType == typeof(FallbackScope))
                {
                    yield return RegistrationBuilder.ForDelegate(serviceType, (context, p) =>
                    {
                        var lifetime = context.Resolve<ILifetimeScope>() as ISharingLifetimeScope;

                        if (lifetime != null)
                        {
                            var parentLifetime = lifetime.ParentLifetimeScope;

                            FallbackScope parentFallback;
                            if (parentLifetime != null &&
                                parentLifetime.TryResolve<FallbackScope>(out parentFallback))
                            {
                                var scopeFactory = parentFallback.ServiceProvider
                                    .GetServiceOrDefault<IServiceScopeFactory>();

                                if (scopeFactory != null)
                                {
                                    return new FallbackScope(scopeFactory.CreateScope());
                                }
                            }
                        }

                        return new FallbackScope(_fallbackServiceProvider);
                    })
                    .InstancePerLifetimeScope()
                    .CreateRegistration();
                }
                else if (_fallbackServiceProvider.HasService(serviceType))
                {
                    yield return RegistrationBuilder.ForDelegate(serviceType, (context, p) =>
                    {
                        var fallbackScope = context.Resolve<FallbackScope>();
                        return fallbackScope.ServiceProvider.GetService(serviceType);
                    })
                    .PreserveExistingDefaults()
                    .CreateRegistration();
                }
            }
        }

        private class FallbackScope : IDisposable
        {
            private readonly IDisposable _scopeDisposer;

            public FallbackScope(IServiceProvider fallbackServiceProvider)
                : this(fallbackServiceProvider, scopeDisposer: null)
            {
            }
            public FallbackScope(IServiceScope fallbackScope)
                : this(fallbackScope.ServiceProvider, fallbackScope)
            {
            }

            private FallbackScope(IServiceProvider fallbackServiceProvider, IDisposable scopeDisposer)
            {
                ServiceProvider = fallbackServiceProvider;
                _scopeDisposer = scopeDisposer;
            }

            public IServiceProvider ServiceProvider { get; private set; }

            public void Dispose()
            {
                if (_scopeDisposer != null)
                {
                    _scopeDisposer.Dispose();
                }
            }
        }
    }
}
