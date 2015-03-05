// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.DependencyInjection
{
    public class ServiceDescriber
    {
        private IConfiguration _configuration;

        public ServiceDescriber(IConfiguration configuration = null)
        {
            _configuration = configuration ?? new Configuration();
        }

        public ServiceDescriptor Transient<TService, TImplementation>()
            where TImplementation : TService
        {
            return Describe<TService, TImplementation>(ServiceLifetime.Transient);
        }

        public ServiceDescriptor Transient([NotNull] Type service, [NotNull] Type implementationType)
        {
            return Describe(service, implementationType, ServiceLifetime.Transient);
        }

        public ServiceDescriptor Transient<TService>([NotNull] Func<IServiceProvider, TService> implementationFactory)
            where TService : class
        {
            return Describe(typeof(TService), implementationFactory, ServiceLifetime.Transient);
        }

        public ServiceDescriptor Transient([NotNull] Type service,
                                           [NotNull] Func<IServiceProvider, object> implementationFactory)
        {
            return Describe(service, implementationFactory, ServiceLifetime.Transient);
        }

        public ServiceDescriptor Scoped<TService, TImplementation>()
            where TImplementation : TService
        {
            return Describe<TService, TImplementation>(ServiceLifetime.Scoped);
        }

        public ServiceDescriptor Scoped(Type service, Type implementationType)
        {
            return Describe(service, implementationType, ServiceLifetime.Scoped);
        }

        public ServiceDescriptor Scoped<TService>([NotNull] Func<IServiceProvider, TService> implementationFactory)
            where TService : class
        {
            return Describe(typeof(TService), implementationFactory, ServiceLifetime.Scoped);
        }

        public ServiceDescriptor Scoped([NotNull] Type service,
                                        [NotNull] Func<IServiceProvider, object> implementationFactory)
        {
            return Describe(service, implementationFactory, ServiceLifetime.Scoped);
        }

        public ServiceDescriptor Singleton<TService, TImplementation>()
            where TImplementation : TService
        {
            return Describe<TService, TImplementation>(ServiceLifetime.Singleton);
        }

        public ServiceDescriptor Singleton([NotNull] Type service, [NotNull] Type implementationType)
        {
            return Describe(service, implementationType, ServiceLifetime.Singleton);
        }

        public ServiceDescriptor Singleton<TService>([NotNull] Func<IServiceProvider, TService> implementationFactory)
            where TService : class
        {
            return Describe(typeof(TService), implementationFactory, ServiceLifetime.Singleton);
        }

        public ServiceDescriptor Singleton([NotNull] Type serviceType,
                                           [NotNull] Func<IServiceProvider, object> implementationFactory)
        {
            return Describe(serviceType, implementationFactory, ServiceLifetime.Singleton);
        }

        public ServiceDescriptor Instance<TService>([NotNull] TService implementationInstance)
        {
            return Instance(typeof(TService), implementationInstance);
        }

        public ServiceDescriptor Instance([NotNull] Type serviceType,
                                          [NotNull] object implementationInstance)
        {
            var implementationType = GetRemappedImplementationType(serviceType);
            if (implementationType != null)
            {
                return new ServiceDescriptor(serviceType, implementationType, ServiceLifetime.Singleton);
            }

            return new ServiceDescriptor(serviceType, implementationInstance);
        }

        private ServiceDescriptor Describe<TService, TImplementation>(ServiceLifetime lifetime)
        {
            return Describe(
                typeof(TService),
                typeof(TImplementation),
                lifetime: lifetime);
        }

        public ServiceDescriptor Describe(Type serviceType, Type implementationType, ServiceLifetime lifetime)
        {
            implementationType = GetRemappedImplementationType(serviceType) ?? implementationType;

            return new ServiceDescriptor(serviceType, implementationType, lifetime);
        }

        public ServiceDescriptor Describe(Type serviceType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime lifetime)
        {
            var implementationType = GetRemappedImplementationType(serviceType);
            if (implementationType != null)
            {
                return new ServiceDescriptor(serviceType, implementationType, lifetime);
            }

            return new ServiceDescriptor(serviceType, implementationFactory, lifetime);
        }

        private Type GetRemappedImplementationType(Type serviceType)
        {
            // Allow the user to change the implementation source via configuration.
            var serviceTypeName = serviceType.FullName;
            var implementationTypeName = _configuration.Get(serviceTypeName);
            if (!string.IsNullOrEmpty(implementationTypeName))
            {
                var type = Type.GetType(implementationTypeName, throwOnError: false);
                if (type == null)
                {
                    throw new InvalidOperationException(Resources.FormatCannotLocateImplementation(
                        implementationTypeName,
                        serviceTypeName));
                }

                return type;
            }

            return null;
        }
    }
}
