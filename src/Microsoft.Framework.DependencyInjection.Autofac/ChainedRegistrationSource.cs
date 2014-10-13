// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
            if (serviceWithType == null)
            {
                yield break;
            }

            // Only introduce services that are not already registered
            if (registrationAcessor(service).Any())
            {
                yield break;
            }

            var serviceType = serviceWithType.ServiceType;
            if (serviceType == typeof(FallbackScope))
            {
                // This is where we rescope the _fallbackServiceProvider for use in inner scopes
                // When we actually resolve fallback services, we first access the scoped fallback
                // service provider by resolving FallbackScope and using its ServiceProvider property.
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
                            var scopeFactory = parentFallback.ServiceProvider.GetService<IServiceScopeFactory>();

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
            else if (_fallbackServiceProvider.GetService(serviceType) != null)
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
