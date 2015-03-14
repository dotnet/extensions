// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.Framework.DependencyInjection
{
    public static class OptionsServiceCollectionExtensions
    {
        public static IServiceCollection AddOptions([NotNull]this IServiceCollection services)
        {
            services.TryAdd(ServiceDescriptor.Singleton(typeof(IOptions<>), typeof(OptionsManager<>)));
            return services;
        }

        private static bool IsAction(Type type)
        {
            return (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Action<>));
        }

        private static IEnumerable<Type> FindIConfigureOptions(Type type)
        {
            var serviceTypes = type.GetTypeInfo().ImplementedInterfaces
                .Where(t => t.GetTypeInfo().IsGenericType && t.GetGenericTypeDefinition() == typeof(IConfigureOptions<>));
            if (!serviceTypes.Any())
            {
                string error = "TODO: No IConfigureOptions<> found.";
                if (IsAction(type))
                {
                    error += " did you mean Configure(Action<T>)";
                }
                throw new InvalidOperationException(error);
            }
            return serviceTypes;
        }

        public static IServiceCollection ConfigureOptions([NotNull]this IServiceCollection services, Type configureType)
        {
            var serviceTypes = FindIConfigureOptions(configureType);
            foreach (var serviceType in serviceTypes)
            {
                services.AddTransient(serviceType, configureType);
            }
            return services;
        }

        public static IServiceCollection ConfigureOptions<TSetup>([NotNull]this IServiceCollection services)
        {
            return services.ConfigureOptions(typeof(TSetup));
        }

        public static IServiceCollection ConfigureOptions([NotNull]this IServiceCollection services, [NotNull]object configureInstance)
        {
            var serviceTypes = FindIConfigureOptions(configureInstance.GetType());
            foreach (var serviceType in serviceTypes)
            {
                services.AddInstance(serviceType, configureInstance);
            }
            return services;
        }

        public static IServiceCollection Configure<TOptions>([NotNull]this IServiceCollection services,
            [NotNull] Action<TOptions> setupAction,
            string optionsName)
        {
            return services.Configure(setupAction, OptionsConstants.DefaultOrder, optionsName);
        }

        public static IServiceCollection Configure<TOptions>([NotNull]this IServiceCollection services,
            [NotNull] Action<TOptions> setupAction,
            int order = OptionsConstants.DefaultOrder,
            string optionsName = "")
        {
            services.ConfigureOptions(new ConfigureOptions<TOptions>(setupAction)
            {
                Name = optionsName,
                Order = order
            });
            return services;
        }

        public static IServiceCollection Configure<TOptions>([NotNull]this IServiceCollection services,
            [NotNull] IConfiguration config, string optionsName)
        {
            return services.Configure<TOptions>(config, OptionsConstants.ConfigurationOrder, optionsName);
        }

        public static IServiceCollection Configure<TOptions>([NotNull]this IServiceCollection services,
            [NotNull] IConfiguration config,
            int order = OptionsConstants.ConfigurationOrder, 
            string optionsName = "")
        {
            services.ConfigureOptions(new ConfigureFromConfigurationOptions<TOptions>(config)
            {
                Name = optionsName,
                Order = order
            });
            return services;
        }
    }
}