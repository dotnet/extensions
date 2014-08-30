// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.ConfigurationModel;

namespace Microsoft.Framework.DependencyInjection
{
    public class ServiceDescriber
    {
        private IConfiguration _configuration;

        public ServiceDescriber()
            : this(new Configuration())
        {
        }

        public ServiceDescriber(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public ServiceDescriptor Transient<TService, TImplementation>()
        {
            return Describe<TService, TImplementation>(LifecycleKind.Transient);
        }

        public ServiceDescriptor Transient(Type service, Type implementationType)
        {
            return Describe(service, implementationType, LifecycleKind.Transient);
        }

        public ServiceDescriptor Transient<TService>(Func<IServiceProvider, object> implementationFactory)
        {
            return Describe(typeof(TService), implementationFactory, LifecycleKind.Transient);
        }

        public ServiceDescriptor Transient(Type service, Func<IServiceProvider, object> implementationFactory)
        {
            return Describe(service, implementationFactory, LifecycleKind.Transient);
        }

        public ServiceDescriptor Scoped<TService, TImplementation>()
        {
            return Describe<TService, TImplementation>(LifecycleKind.Scoped);
        }

        public ServiceDescriptor Scoped(Type service, Type implementationType)
        {
            return Describe(service, implementationType, LifecycleKind.Scoped);
        }

        public ServiceDescriptor Scoped<TService>(Func<IServiceProvider, object> implementationFactory)
        {
            return Describe(typeof(TService), implementationFactory, LifecycleKind.Scoped);
        }

        public ServiceDescriptor Scoped(Type service, Func<IServiceProvider, object> implementationFactory)
        {
            return Describe(service, implementationFactory, LifecycleKind.Scoped);
        }

        public ServiceDescriptor Singleton<TService, TImplementation>()
        {
            return Describe<TService, TImplementation>(LifecycleKind.Singleton);
        }

        public ServiceDescriptor Singleton(Type service, Type implementationType)
        {
            return Describe(service, implementationType, LifecycleKind.Singleton);
        }

        public ServiceDescriptor Singleton<TService>(Func<IServiceProvider, object> implementationFactory)
        {
            return Describe(typeof(TService), implementationFactory, LifecycleKind.Singleton);
        }

        public ServiceDescriptor Singleton(Type serviceType, Func<IServiceProvider, object> implementationFactory)
        {
            return Describe(serviceType, implementationFactory, LifecycleKind.Singleton);
        }

        public ServiceDescriptor Instance<TService>(object implementationInstance)
        {
            return Instance(typeof(TService), implementationInstance);
        }

        public ServiceDescriptor Instance(Type serviceType, object implementationInstance)
        {
            var implementationType = GetRemappedImplementationType(serviceType);
            if (implementationType != null)
            {
                return new ServiceDescriptor(serviceType, implementationType, LifecycleKind.Singleton);
            }

            return new ServiceDescriptor(serviceType, implementationInstance);
        }

        private ServiceDescriptor Describe<TService, TImplementation>(LifecycleKind lifecycle)
        {
            return Describe(
                typeof(TService),
                typeof(TImplementation),
                lifecycle: lifecycle);
        }

        public ServiceDescriptor Describe(Type serviceType, Type implementationType, LifecycleKind lifecycle)
        {
            implementationType = GetRemappedImplementationType(serviceType) ?? implementationType;

            return new ServiceDescriptor(serviceType, implementationType, lifecycle);
        }

        public ServiceDescriptor Describe(Type serviceType, Func<IServiceProvider, object> implementationFactory, LifecycleKind lifecycle)
        {
            var implementationType = GetRemappedImplementationType(serviceType);
            if (implementationType != null)
            {
                return new ServiceDescriptor(serviceType, implementationType, lifecycle);
            }

            return new ServiceDescriptor(serviceType, implementationFactory, lifecycle);
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
                    var message = string.Format("TODO: unable to locate implementation {0} for service {1}", implementationTypeName, serviceTypeName);
                    throw new InvalidOperationException(message);
                }

                return type;
            }

            return null;
        }
    }
}
