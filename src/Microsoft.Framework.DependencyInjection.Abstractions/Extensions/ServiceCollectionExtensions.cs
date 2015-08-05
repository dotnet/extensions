// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.DependencyInjection.Abstractions;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.DependencyInjection.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the specified <paramref name="descriptor"/> to the <paramref name="collection"/>.
        /// </summary>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="descriptor">The <see cref="ServiceDescriptor"/>.</param>
        /// <returns>A reference to the current instance of <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection Add([NotNull] this IServiceCollection collection,
                                             [NotNull] ServiceDescriptor descriptor)
        {
            collection.Add(descriptor);
            return collection;
        }

        /// <summary>
        /// Adds a sequence of <see cref="ServiceDescriptor"/> to the <paramref name="collection"/>.
        /// </summary>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="descriptors">The <see cref="IEnumerable{T}"/> of <see cref="ServiceDescriptor"/>s to add.</param>
        /// <returns>A reference to the current instance of <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection Add([NotNull] this IServiceCollection collection,
                                             [NotNull] IEnumerable<ServiceDescriptor> descriptors)
        {
            foreach (var descriptor in descriptors)
            {
                collection.Add(descriptor);
            }

            return collection;
        }

        /// <summary>
        /// Adds the specified <paramref name="descriptor"/> to the <paramref name="collection"/> if the
        /// service type hasn't been already registered.
        /// </summary>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="descriptor">The <see cref="ServiceDescriptor"/>.</param>
        public static void TryAdd([NotNull] this IServiceCollection collection,
                                  [NotNull] ServiceDescriptor descriptor)
        {
            if (!collection.Any(d => d.ServiceType == descriptor.ServiceType))
            {
                collection.Add(descriptor);
            }
        }

        /// <summary>
        /// Adds the specified <paramref name="descriptors"/> to the <paramref name="collection"/> if the
        /// service type hasn't been already registered.
        /// </summary>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="descriptors">The <see cref="ServiceDescriptor"/>s.</param>
        public static void TryAdd([NotNull] this IServiceCollection collection,
                                  [NotNull] IEnumerable<ServiceDescriptor> descriptors)
        {
            foreach (var d in descriptors)
            {
                collection.TryAdd(d);
            }
        }

        public static void TryAddTransient(
            [NotNull] this IServiceCollection collection,
            [NotNull] Type service)
        {
            var descriptor = ServiceDescriptor.Transient(service, service);
            TryAdd(collection, descriptor);
        }

        public static void TryAddTransient(
            [NotNull] this IServiceCollection collection,
            [NotNull] Type service,
            [NotNull] Type implementationType)
        {
            var descriptor = ServiceDescriptor.Transient(service, implementationType);
            TryAdd(collection, descriptor);
        }

        public static void TryAddTransient(
            [NotNull] this IServiceCollection collection,
            [NotNull] Type service,
            [NotNull] Func<IServiceProvider, object> implementationFactory)
        {
            var descriptor = ServiceDescriptor.Transient(service, implementationFactory);
            TryAdd(collection, descriptor);
        }

        public static void TryAddTransient<TService>([NotNull] this IServiceCollection collection)
            where TService : class
        {
            TryAddTransient(collection, typeof(TService), typeof(TService));
        }

        public static void TryAddTransient<TService, TImplementation>([NotNull] this IServiceCollection collection)
            where TService : class
            where TImplementation : class, TService
        {
            TryAddTransient(collection, typeof(TService), typeof(TImplementation));
        }

        public static void TryAddScoped(
            [NotNull] this IServiceCollection collection,
            [NotNull] Type service)
        {
            var descriptor = ServiceDescriptor.Scoped(service, service);
            TryAdd(collection, descriptor);
        }

        public static void TryAddScoped(
            [NotNull] this IServiceCollection collection,
            [NotNull] Type service,
            [NotNull] Type implementationType)
        {
            var descriptor = ServiceDescriptor.Scoped(service, implementationType);
            TryAdd(collection, descriptor);
        }

        public static void TryAddScoped(
            [NotNull] this IServiceCollection collection,
            [NotNull] Type service,
            [NotNull] Func<IServiceProvider, object> implementationFactory)
        {
            var descriptor = ServiceDescriptor.Scoped(service, implementationFactory);
            TryAdd(collection, descriptor);
        }

        public static void TryAddScoped<TService>([NotNull] this IServiceCollection collection)
            where TService : class
        {
            TryAddScoped(collection, typeof(TService), typeof(TService));
        }

        public static void TryAddScoped<TService, TImplementation>([NotNull] this IServiceCollection collection)
            where TService : class
            where TImplementation : class, TService
        {
            TryAddScoped(collection, typeof(TService), typeof(TImplementation));
        }

        public static void TryAddSingleton(
            [NotNull] this IServiceCollection collection,
            [NotNull] Type service)
        {
            var descriptor = ServiceDescriptor.Singleton(service, service);
            TryAdd(collection, descriptor);
        }

        public static void TryAddSingleton(
            [NotNull] this IServiceCollection collection,
            [NotNull] Type service,
            [NotNull] Type implementationType)
        {
            var descriptor = ServiceDescriptor.Singleton(service, implementationType);
            TryAdd(collection, descriptor);
        }

        public static void TryAddSingleton(
            [NotNull] this IServiceCollection collection,
            [NotNull] Type service,
            [NotNull] Func<IServiceProvider, object> implementationFactory)
        {
            var descriptor = ServiceDescriptor.Singleton(service, implementationFactory);
            TryAdd(collection, descriptor);
        }

        public static void TryAddSingleton<TService>([NotNull] this IServiceCollection collection)
            where TService : class
        {
            TryAddSingleton(collection, typeof(TService), typeof(TService));
        }

        public static void TryAddSingleton<TService, TImplementation>([NotNull] this IServiceCollection collection)
            where TService : class
            where TImplementation : class, TService
        {
            TryAddSingleton(collection, typeof(TService), typeof(TImplementation));
        }

        /// <summary>
        /// Adds a <see cref="ServiceDescriptor"/> if an existing descriptor with the same
        /// <see cref="ServiceDescriptor.ServiceType"/> and an implementation that does not already exist
        /// in <paramref name="services."/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="descriptor">The <see cref="ServiceDescriptor"/>.</param>
        /// <remarks>
        /// Use <see cref="TryAddEnumerable(IServiceCollection, ServiceDescriptor)"/> when registing a service implementation of a
        /// service type that
        /// supports multiple registrations of the same service type. Using
        /// <see cref="Add(IServiceCollection, ServiceDescriptor)"/> is not idempotent and can add
        /// duplicate
        /// <see cref="ServiceDescriptor"/> instances if called twice. Using
        /// <see cref="TryAddEnumerable(IServiceCollection, ServiceDescriptor)"/> will prevent registration
        /// of multiple implementation types.
        /// </remarks>
        public static void TryAddEnumerable(
            [NotNull] this IServiceCollection services,
            [NotNull] ServiceDescriptor descriptor)
        {
            var implementationType = descriptor.GetImplementationType();

            if (implementationType == typeof(object) ||
                implementationType == descriptor.ServiceType)
            {
                throw new ArgumentException(
                    Resources.FormatTryAddIndistinguishableTypeToEnumerable(
                        implementationType,
                        descriptor.ServiceType),
                    nameof(descriptor));
            }

            if (!services.Any(d =>
                              d.ServiceType == descriptor.ServiceType &&
                              d.GetImplementationType() == implementationType))
            {
                services.Add(descriptor);
            }
        }

        /// <summary>
        /// Adds the specified <see cref="ServiceDescriptor"/>s if an existing descriptor with the same
        /// <see cref="ServiceDescriptor.ServiceType"/> and an implementation that does not already exist
        /// in <paramref name="services."/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="descriptors">The <see cref="ServiceDescriptor"/>s.</param>
        /// <remarks>
        /// Use <see cref="TryAddEnumerable(IServiceCollection, ServiceDescriptor)"/> when registing a service
        /// implementation of a service type that
        /// supports multiple registrations of the same service type. Using
        /// <see cref="Add(IServiceCollection, ServiceDescriptor)"/> is not idempotent and can add
        /// duplicate
        /// <see cref="ServiceDescriptor"/> instances if called twice. Using
        /// <see cref="TryAddEnumerable(IServiceCollection, ServiceDescriptor)"/> will prevent registration
        /// of multiple implementation types.
        /// </remarks>
        public static void TryAddEnumerable(
            [NotNull] this IServiceCollection services,
            [NotNull] IEnumerable<ServiceDescriptor> descriptors)
        {
            foreach (var d in descriptors)
            {
                services.TryAddEnumerable(d);
            }
        }

        /// <summary>
        /// Removes the first service in <see cref="IServiceCollection"/> with the same service type
        /// as <paramref name="descriptor"/> and adds <paramef name="descriptor"/> to the collection.
        /// </summary>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="descriptor">The <see cref="ServiceDescriptor"/> to replace with.</param>
        /// <returns></returns>
        public static IServiceCollection Replace([NotNull] this IServiceCollection collection,
                                                 [NotNull] ServiceDescriptor descriptor)
        {
            var registeredServiceDescriptor = collection.FirstOrDefault(s => s.ServiceType == descriptor.ServiceType);
            if (registeredServiceDescriptor != null)
            {
                collection.Remove(registeredServiceDescriptor);
            }

            collection.Add(descriptor);
            return collection;
        }
    }
}
