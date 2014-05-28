// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.Framework.DependencyInjection
{
    public static class OptionsServiceCollectionExtensions
    {
        public static IServiceCollection AddSetup([NotNull]this IServiceCollection services, Type setupType)
        {
            var serviceTypes = setupType.GetTypeInfo().ImplementedInterfaces
                .Where(t => t.GetTypeInfo().IsGenericType && t.GetGenericTypeDefinition() == typeof(IOptionsSetup<>));
            foreach (var serviceType in serviceTypes)
            {
                services.Add(new ServiceDescriptor
                {
                    ServiceType = serviceType,
                    ImplementationType = setupType,
                    Lifecycle = LifecycleKind.Transient
                });
            }
            return services;
        }

        public static IServiceCollection AddSetup<TSetup>([NotNull]this IServiceCollection services)
        {
            return services.AddSetup(typeof(TSetup));
        }

        public static IServiceCollection AddSetup([NotNull]this IServiceCollection services, [NotNull]object setupInstance)
        {
            var setupType = setupInstance.GetType();
            var serviceTypes = setupType.GetTypeInfo().ImplementedInterfaces
                .Where(t => t.GetTypeInfo().IsGenericType && t.GetGenericTypeDefinition() == typeof(IOptionsSetup<>));
            foreach (var serviceType in serviceTypes)
            {
                services.Add(new ServiceDescriptor
                {
                    ServiceType = serviceType,
                    ImplementationInstance = setupInstance,
                    Lifecycle = LifecycleKind.Singleton
                });
            }
            return services;
        }

        public static IServiceCollection SetupOptions<TOptions>([NotNull]this IServiceCollection services,
            Action<TOptions> setupAction,
            int order)
        {
            services.AddSetup(new OptionsSetup<TOptions>(setupAction) { Order = order });
            return services;
        }

        public static IServiceCollection SetupOptions<TOptions>([NotNull]this IServiceCollection services,
            Action<TOptions> setupAction)
        {
            return services.SetupOptions(setupAction, order: OptionsConstants.DefaultOrder);
        }

        public static IServiceCollection SetupOptions<TOptions>([NotNull]this IServiceCollection services,
            IConfiguration config,
            int order)
        {
            services.AddSetup(new ConfigOptionsSetup<TOptions>(config, order));
            return services;
        }

        public static IServiceCollection SetupOptions<TOptions>([NotNull]this IServiceCollection services,
            IConfiguration config)
        {
            return services.SetupOptions<TOptions>(config, OptionsConstants.ConfigurationOrder);
        }
    }
}
