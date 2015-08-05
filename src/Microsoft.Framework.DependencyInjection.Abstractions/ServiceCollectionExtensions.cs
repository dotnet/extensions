// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTransient([NotNull] this IServiceCollection collection,
                                                      [NotNull] Type service,
                                                      [NotNull] Type implementationType)
        {
            return Add(collection, service, implementationType, ServiceLifetime.Transient);
        }

        public static IServiceCollection AddTransient([NotNull] this IServiceCollection collection,
                                                      [NotNull] Type service,
                                                      [NotNull] Func<IServiceProvider, object> implementationFactory)
        {
            return Add(collection, service, implementationFactory, ServiceLifetime.Transient);
        }

        public static IServiceCollection AddScoped([NotNull] this IServiceCollection collection,
                                                   [NotNull] Type service,
                                                   [NotNull] Type implementationType)
        {
            return Add(collection, service, implementationType, ServiceLifetime.Scoped);
        }

        public static IServiceCollection AddScoped([NotNull] this IServiceCollection collection,
                                                   [NotNull] Type service,
                                                   [NotNull] Func<IServiceProvider, object> implementationFactory)
        {
            return Add(collection, service, implementationFactory, ServiceLifetime.Scoped);
        }

        public static IServiceCollection AddSingleton([NotNull] this IServiceCollection collection,
                                                      [NotNull] Type service,
                                                      [NotNull] Type implementationType)
        {
            return Add(collection, service, implementationType, ServiceLifetime.Singleton);
        }

        public static IServiceCollection AddSingleton([NotNull] this IServiceCollection collection,
                                                      [NotNull] Type service,
                                                      [NotNull] Func<IServiceProvider, object> implementationFactory)
        {
            return Add(collection, service, implementationFactory, ServiceLifetime.Singleton);
        }

        public static IServiceCollection AddInstance([NotNull] this IServiceCollection collection,
                                                     [NotNull] Type service,
                                                     [NotNull] object implementationInstance)
        {
            var serviceDescriptor = new ServiceDescriptor(service, implementationInstance);
            collection.Add(serviceDescriptor);
            return collection;
        }

        public static IServiceCollection AddTransient<TService, TImplementation>([NotNull] this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            return services.AddTransient(typeof(TService), typeof(TImplementation));
        }

        public static IServiceCollection AddTransient([NotNull] this IServiceCollection services,
                                                      [NotNull] Type serviceType)
        {
            return services.AddTransient(serviceType, serviceType);
        }

        public static IServiceCollection AddTransient<TService>([NotNull] this IServiceCollection services)
            where TService : class
        {
            return services.AddTransient(typeof(TService));
        }

        public static IServiceCollection AddTransient<TService>([NotNull] this IServiceCollection services,
                                                                [NotNull] Func<IServiceProvider, TService> implementationFactory)
            where TService : class
        {
            return services.AddTransient(typeof(TService), implementationFactory);
        }

        public static IServiceCollection AddTransient<TService, TImplementation>(
            [NotNull] this IServiceCollection services,
            [NotNull] Func<IServiceProvider, TImplementation> implementationFactory)
            where TService : class
            where TImplementation : class, TService
        {
            return services.AddTransient(typeof(TService), implementationFactory);
        }

        public static IServiceCollection AddScoped<TService, TImplementation>([NotNull] this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            return services.AddScoped(typeof(TService), typeof(TImplementation));
        }

        public static IServiceCollection AddScoped([NotNull] this IServiceCollection services,
                                                   [NotNull] Type serviceType)
        {
            return services.AddScoped(serviceType, serviceType);
        }

        public static IServiceCollection AddScoped<TService>([NotNull] this IServiceCollection services,
                                                             [NotNull] Func<IServiceProvider, TService> implementationFactory)
            where TService : class
        {
            return services.AddScoped(typeof(TService), implementationFactory);
        }

        public static IServiceCollection AddScoped<TService, TImplementation>(
            [NotNull] this IServiceCollection services,
            [NotNull] Func<IServiceProvider, TImplementation> implementationFactory)
            where TService : class
            where TImplementation : class, TService
        {
            return services.AddScoped(typeof(TService), implementationFactory);
        }

        public static IServiceCollection AddScoped<TService>([NotNull] this IServiceCollection services)
            where TService : class
        {
            return services.AddScoped(typeof(TService));
        }

        public static IServiceCollection AddSingleton<TService, TImplementation>([NotNull] this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            return services.AddSingleton(typeof(TService), typeof(TImplementation));
        }

        public static IServiceCollection AddSingleton([NotNull] this IServiceCollection services,
                                                      [NotNull] Type serviceType)
        {
            return services.AddSingleton(serviceType, serviceType);
        }

        public static IServiceCollection AddSingleton<TService>([NotNull] this IServiceCollection services)
            where TService : class
        {
            return services.AddSingleton(typeof(TService));
        }

        public static IServiceCollection AddSingleton<TService>([NotNull] this IServiceCollection services,
                                                                [NotNull] Func<IServiceProvider, TService> implementationFactory)
            where TService : class
        {
            return services.AddSingleton(typeof(TService), implementationFactory);
        }

        public static IServiceCollection AddSingleton<TService, TImplementation>(
            [NotNull] this IServiceCollection services,
            [NotNull] Func<IServiceProvider, TImplementation> implementationFactory)
            where TService : class
            where TImplementation : class, TService
        {
            return services.AddSingleton(typeof(TService), implementationFactory);
        }


        public static IServiceCollection AddInstance<TService>([NotNull] this IServiceCollection services,
                                                               [NotNull] TService implementationInstance)
            where TService : class
        {
            return services.AddInstance(typeof(TService), implementationInstance);
        }

        private static IServiceCollection Add(IServiceCollection collection,
                                              Type service,
                                              Type implementationType,
                                              ServiceLifetime lifeCycle)
        {
            var descriptor = new ServiceDescriptor(service, implementationType, lifeCycle);
            collection.Add(descriptor);
            return collection;
        }

        private static IServiceCollection Add(IServiceCollection collection,
                                              Type service,
                                              Func<IServiceProvider, object> implementationFactory,
                                              ServiceLifetime lifeCycle)
        {
            var descriptor = new ServiceDescriptor(service, implementationFactory, lifeCycle);
            collection.Add(descriptor);
            return collection;
        }
    }
}
