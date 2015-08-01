// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Framework.DependencyInjection.Abstractions;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.DependencyInjection
{
    [DebuggerDisplay("Lifetime = {Lifetime}, ServiceType = {ServiceType}, ImplementationType = {ImplementationType}")]
    public class ServiceDescriptor
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ServiceDescriptor"/> with the specified <paramref name="implementationType"/>.
        /// </summary>
        /// <param name="serviceType">The <see cref="Type"/> of the service.</param>
        /// <param name="implementationType">The <see cref="Type"/> implementing the service.</param>
        /// <param name="lifetime">The <see cref="ServiceLifetime"/> of the service.</param>
        public ServiceDescriptor([NotNull] Type serviceType,
                                 [NotNull] Type implementationType,
                                 ServiceLifetime lifetime)
            : this(serviceType, lifetime)
        {
            ImplementationType = implementationType;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ServiceDescriptor"/> with the specified <paramref name="instance"/>
        /// as a <see cref="ServiceLifetime.Singleton"/>.
        /// </summary>
        /// <param name="serviceType">The <see cref="Type"/> of the service.</param>
        /// <param name="instance">The instance implementing the service.</param>
        public ServiceDescriptor([NotNull] Type serviceType,
                                 [NotNull] object instance)
            : this(serviceType, ServiceLifetime.Singleton)
        {
            ImplementationInstance = instance;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ServiceDescriptor"/> with the specified <paramref name="factory"/>.
        /// </summary>
        /// <param name="serviceType">The <see cref="Type"/> of the service.</param>
        /// <param name="factory">A factory used for creating service instances.</param>
        /// <param name="lifetime">The <see cref="ServiceLifetime"/> of the service.</param>
        public ServiceDescriptor([NotNull] Type serviceType,
                                 [NotNull] Func<IServiceProvider, object> factory,
                                 ServiceLifetime lifetime)
            : this(serviceType, lifetime)
        {
            ImplementationFactory = factory;
        }

        private ServiceDescriptor(Type serviceType, ServiceLifetime lifetime)
        {
            Lifetime = lifetime;
            ServiceType = serviceType;
        }

        /// <inheritdoc />
        public ServiceLifetime Lifetime { get; }

        /// <inheritdoc />
        public Type ServiceType { get; }

        /// <inheritdoc />
        public Type ImplementationType { get; }

        /// <inheritdoc />
        public object ImplementationInstance { get; }

        /// <inheritdoc />
        public Func<IServiceProvider, object> ImplementationFactory { get; }

        internal Type GetImplementationType()
        {
            if (ImplementationType != null)
            {
                return ImplementationType;
            }
            else if (ImplementationInstance != null)
            {
                return ImplementationInstance.GetType();
            }
            else if (ImplementationFactory != null)
            {
                var typeArguments = ImplementationFactory.GetType().GenericTypeArguments;

                Debug.Assert(typeArguments.Length == 2);

                return typeArguments[1];
            }

            throw new ArgumentException(Resources.FormatNoImplementation(ServiceType));
        }

        public static ServiceDescriptor Transient<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            return Describe<TService, TImplementation>(ServiceLifetime.Transient);
        }

        public static ServiceDescriptor Transient([NotNull] Type service, [NotNull] Type implementationType)
        {
            return Describe(service, implementationType, ServiceLifetime.Transient);
        }

        public static ServiceDescriptor Transient<TService, TImplementation>(
            [NotNull] Func<IServiceProvider, TImplementation> implementationFactory)
            where TService : class
            where TImplementation : class, TService
        {
            return Describe(typeof(TService), implementationFactory, ServiceLifetime.Transient);
        }

        public static ServiceDescriptor Transient<TService>([NotNull] Func<IServiceProvider, TService> implementationFactory)
            where TService : class
        {
            return Describe(typeof(TService), implementationFactory, ServiceLifetime.Transient);
        }

        public static ServiceDescriptor Transient([NotNull] Type service,
                                                  [NotNull] Func<IServiceProvider, object> implementationFactory)
        {
            return Describe(service, implementationFactory, ServiceLifetime.Transient);
        }

        public static ServiceDescriptor Scoped<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            return Describe<TService, TImplementation>(ServiceLifetime.Scoped);
        }

        public static ServiceDescriptor Scoped(Type service, Type implementationType)
        {
            return Describe(service, implementationType, ServiceLifetime.Scoped);
        }

        public static ServiceDescriptor Scoped<TService, TImplementation>(
            [NotNull] Func<IServiceProvider, TImplementation> implementationFactory)
            where TService : class
            where TImplementation : class, TService
        {
            return Describe(typeof(TService), implementationFactory, ServiceLifetime.Scoped);
        }

        public static ServiceDescriptor Scoped<TService>([NotNull] Func<IServiceProvider, TService> implementationFactory)
            where TService : class
        {
            return Describe(typeof(TService), implementationFactory, ServiceLifetime.Scoped);
        }

        public static ServiceDescriptor Scoped([NotNull] Type service,
                                               [NotNull] Func<IServiceProvider, object> implementationFactory)
        {
            return Describe(service, implementationFactory, ServiceLifetime.Scoped);
        }

        public static ServiceDescriptor Singleton<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            return Describe<TService, TImplementation>(ServiceLifetime.Singleton);
        }

        public static ServiceDescriptor Singleton([NotNull] Type service, [NotNull] Type implementationType)
        {
            return Describe(service, implementationType, ServiceLifetime.Singleton);
        }

        public static ServiceDescriptor Singleton<TService, TImplementation>(
            [NotNull] Func<IServiceProvider, TImplementation> implementationFactory)
            where TService : class
            where TImplementation : class, TService
        {
            return Describe(typeof(TService), implementationFactory, ServiceLifetime.Singleton);
        }

        public static ServiceDescriptor Singleton<TService>([NotNull] Func<IServiceProvider, TService> implementationFactory)
            where TService : class
        {
            return Describe(typeof(TService), implementationFactory, ServiceLifetime.Singleton);
        }

        public static ServiceDescriptor Singleton([NotNull] Type serviceType,
                                                  [NotNull] Func<IServiceProvider, object> implementationFactory)
        {
            return Describe(serviceType, implementationFactory, ServiceLifetime.Singleton);
        }

        public static ServiceDescriptor Instance<TService>([NotNull] TService implementationInstance)
            where TService : class
        {
            return Instance(typeof(TService), implementationInstance);
        }

        public static ServiceDescriptor Instance([NotNull] Type serviceType,
                                                 [NotNull] object implementationInstance)
        {
            return new ServiceDescriptor(serviceType, implementationInstance);
        }

        private static ServiceDescriptor Describe<TService, TImplementation>(ServiceLifetime lifetime)
            where TService : class
            where TImplementation : class, TService
        {
            return Describe(
                typeof(TService),
                typeof(TImplementation),
                lifetime: lifetime);
        }

        public static ServiceDescriptor Describe(Type serviceType, Type implementationType, ServiceLifetime lifetime)
        {
            return new ServiceDescriptor(serviceType, implementationType, lifetime);
        }

        public static ServiceDescriptor Describe(Type serviceType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime lifetime)
        {
            return new ServiceDescriptor(serviceType, implementationFactory, lifetime);
        }
    }
}
