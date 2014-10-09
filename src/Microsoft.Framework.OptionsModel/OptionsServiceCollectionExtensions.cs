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
        public static IServiceCollection AddOptionsAction([NotNull]this IServiceCollection services, Type configureType)
        {
            var serviceTypes = configureType.GetTypeInfo().ImplementedInterfaces
                .Where(t => t.GetTypeInfo().IsGenericType && t.GetGenericTypeDefinition() == typeof(IOptionsAction<>));
            foreach (var serviceType in serviceTypes)
            {
                services.AddTransient(serviceType, configureType);
            }
            // TODO: consider throwing if we add no services?
            return services;
        }

        public static IServiceCollection AddOptionsAction<TSetup>([NotNull]this IServiceCollection services)
        {
            return services.AddOptionsAction(typeof(TSetup));
        }

        public static IServiceCollection AddOptionsAction([NotNull]this IServiceCollection services, [NotNull]object configureInstance)
        {
            var setupType = configureInstance.GetType();
            var serviceTypes = setupType.GetTypeInfo().ImplementedInterfaces
                .Where(t => t.GetTypeInfo().IsGenericType && t.GetGenericTypeDefinition() == typeof(IOptionsAction<>));
            foreach (var serviceType in serviceTypes)
            {
                services.AddInstance(serviceType, configureInstance);
            }
            // TODO: consider throwing if we add no services?
            return services;
        }

        [Obsolete("Use ConfigureOptions instead")]
        public static IServiceCollection SetupOptions<TOptions>([NotNull]this IServiceCollection services,
            Action<TOptions> setupAction)
        {
            return services.ConfigureOptions(setupAction);
        }

        public static IServiceCollection ConfigureOptions<TOptions>([NotNull]this IServiceCollection services,
            Action<TOptions> setupAction,
            string optionsName)
        {
            return services.ConfigureOptions(setupAction, OptionsConstants.DefaultOrder, optionsName);
        }

        public static IServiceCollection ConfigureOptions<TOptions>([NotNull]this IServiceCollection services,
            [NotNull] Action<TOptions> setupAction,
            int order = OptionsConstants.DefaultOrder,
            string optionsName = "")
        {
            services.AddOptionsAction(new OptionsAction<TOptions>(setupAction)
            {
                Name = optionsName,
                Order = order
            });
        return services;
        }

        public static IServiceCollection ConfigureOptions<TOptions>([NotNull]this IServiceCollection services,
            [NotNull] IConfiguration config, string optionsName)
        {
            return services.ConfigureOptions<TOptions>(config, OptionsConstants.ConfigurationOrder, optionsName);
        }

        public static IServiceCollection ConfigureOptions<TOptions>([NotNull]this IServiceCollection services,
            [NotNull] IConfiguration config,
            int order = OptionsConstants.ConfigurationOrder, 
            string optionsName = "")
        {
            services.AddOptionsAction(new ConfigurationAction<TOptions>(config)
            {
                Name = optionsName,
                Order = order
            });
            return services;
        }
    }
}