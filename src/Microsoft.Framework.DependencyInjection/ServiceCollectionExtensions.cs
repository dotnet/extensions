// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.ConfigurationModel;

namespace Microsoft.Framework.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static bool TryAdd([NotNull] this IServiceCollection collection,
                                  [NotNull] IServiceDescriptor descriptor)
        {
            if (!collection.Any(d => d.ServiceType == descriptor.ServiceType))
            {
                collection.Add(descriptor);
                return true;
            }
            return false;
        }

        public static bool TryAdd([NotNull] this IServiceCollection collection,
                                  [NotNull] IEnumerable<IServiceDescriptor> descriptors)
        {
            bool anyAdded = false;
            foreach (var d in descriptors)
            {
                anyAdded = collection.TryAdd(d) || anyAdded;
            }
            return anyAdded;
        }

        public static IServiceCollection AddTypeActivator([NotNull]this IServiceCollection services, IConfiguration config = null)
        {
            var describe = new ServiceDescriber(config);
            services.TryAdd(describe.Singleton<ITypeActivator, TypeActivator>());
            return services;
        }

        public static IServiceCollection AddContextAccessor([NotNull]this IServiceCollection services, IConfiguration config = null)
        {
            var describe = new ServiceDescriber(config);
            services.TryAdd(describe.Scoped(typeof(IContextAccessor<>), typeof(ContextAccessor<>)));
            return services;
        }

        public static IServiceCollection AddTransient([NotNull] this IServiceCollection collection, 
                                                      [NotNull] Type service, 
                                                      [NotNull] Type implementationType)
        {
            return Add(collection, service, implementationType, LifecycleKind.Transient);
        }

        public static IServiceCollection AddTransient([NotNull] this IServiceCollection collection,
                                                      [NotNull] Type service,
                                                      [NotNull] Func<IServiceProvider, object> implementationFactory)
        {
            return Add(collection, service, implementationFactory, LifecycleKind.Transient);
        }

        public static IServiceCollection AddScoped([NotNull] this IServiceCollection collection,
                                                   [NotNull] Type service,
                                                   [NotNull] Type implementationType)
        {
            return Add(collection, service, implementationType, LifecycleKind.Scoped);
        }

        public static IServiceCollection AddScoped([NotNull] this IServiceCollection collection,
                                                   [NotNull] Type service,
                                                   [NotNull] Func<IServiceProvider, object> implementationFactory)
        {
            return Add(collection, service, implementationFactory, LifecycleKind.Scoped);
        }

        public static IServiceCollection AddSingleton([NotNull] this IServiceCollection collection,
                                                      [NotNull] Type service,
                                                      [NotNull] Type implementationType)
        {
            return Add(collection, service, implementationType, LifecycleKind.Singleton);
        }

        public static IServiceCollection AddSingleton([NotNull] this IServiceCollection collection,
                                                      [NotNull] Type service,
                                                      [NotNull] Func<IServiceProvider, object> implementationFactory)
        {
            return Add(collection, service, implementationFactory, LifecycleKind.Singleton);
        }

        public static IServiceCollection AddInstance([NotNull] this IServiceCollection collection,
                                                     [NotNull] Type service,
                                                     [NotNull] object implementationInstance)
        {
            var serviceDescriptor = new ServiceDescriptor(service, implementationInstance);
            return collection.Add(serviceDescriptor);
        }

        public static IServiceCollection AddTransient<TService, TImplementation>([NotNull] this IServiceCollection services)
            where TImplementation : TService
        {
            return services.AddTransient(typeof(TService), typeof(TImplementation));
        }

        public static IServiceCollection AddTransient([NotNull] this IServiceCollection services,
                                                      [NotNull] Type serviceType)
        {
            return services.AddTransient(serviceType, serviceType);
        }

        public static IServiceCollection AddTransient<TService>([NotNull] this IServiceCollection services)
        {
            return services.AddTransient(typeof(TService));
        }

        public static IServiceCollection AddTransient<TService>([NotNull] this IServiceCollection services,
                                                                [NotNull] Func<IServiceProvider, TService> implementationFactory)
            where TService : class
        {
            return services.AddTransient(typeof(TService), implementationFactory);
        }

        public static IServiceCollection AddScoped<TService, TImplementation>([NotNull] this IServiceCollection services)
            where TImplementation : TService
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

        public static IServiceCollection AddScoped<TService>([NotNull] this IServiceCollection services)
        {
            return services.AddScoped(typeof(TService));
        }

        public static IServiceCollection AddSingleton<TService, TImplementation>([NotNull] this IServiceCollection services)
        {
            return services.AddSingleton(typeof(TService), typeof(TImplementation));
        }

        public static IServiceCollection AddSingleton([NotNull] this IServiceCollection services,
                                                      [NotNull] Type serviceType)
        {
            return services.AddSingleton(serviceType, serviceType);
        }

        public static IServiceCollection AddSingleton<TService>([NotNull] this IServiceCollection services)
        {
            return services.AddSingleton(typeof(TService));
        }

        public static IServiceCollection AddSingleton<TService>([NotNull] this IServiceCollection services,
                                                                [NotNull] Func<IServiceProvider, TService> implementationFactory)
            where TService : class
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
                                              LifecycleKind lifeCycle)
        {
            var descriptor = new ServiceDescriptor(service, implementationType, lifeCycle);
            return collection.Add(descriptor);
        }

        private static IServiceCollection Add(IServiceCollection collection,
                                              Type service,
                                              Func<IServiceProvider, object> implementationFactory,
                                              LifecycleKind lifeCycle)
        {
            var descriptor = new ServiceDescriptor(service, implementationFactory, lifeCycle);
            return collection.Add(descriptor);
        }
    }
}
