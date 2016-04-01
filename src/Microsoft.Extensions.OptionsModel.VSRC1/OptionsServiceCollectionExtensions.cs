// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.OptionsModel.VSRC1;

namespace Microsoft.Extensions.DependencyInjection.VSRC1
{
    public static class OptionsServiceCollectionExtensions
    {
        public static IServiceCollection AddOptions(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

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

        public static IServiceCollection ConfigureOptions(this IServiceCollection services, Type configureType)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var serviceTypes = FindIConfigureOptions(configureType);
            foreach (var serviceType in serviceTypes)
            {
                services.AddTransient(serviceType, configureType);
            }
            return services;
        }

        public static IServiceCollection ConfigureOptions<TSetup>(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.ConfigureOptions(typeof(TSetup));
        }

        public static IServiceCollection ConfigureOptions(this IServiceCollection services, object configureInstance)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configureInstance == null)
            {
                throw new ArgumentNullException(nameof(configureInstance));
            }

            var serviceTypes = FindIConfigureOptions(configureInstance.GetType());
            foreach (var serviceType in serviceTypes)
            {
                services.AddInstance(serviceType, configureInstance);
            }
            return services;
        }

        public static IServiceCollection Configure<TOptions>(
            this IServiceCollection services,
            Action<TOptions> setupAction)
            where TOptions : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            services.ConfigureOptions(new ConfigureOptions<TOptions>(setupAction));
            return services;
        }

        public static IServiceCollection Configure<TOptions>(
            this IServiceCollection services,
            IConfiguration config)
            where TOptions : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            services.ConfigureOptions(new ConfigureFromConfigurationOptions<TOptions>(config));
            return services;
        }
    }
}