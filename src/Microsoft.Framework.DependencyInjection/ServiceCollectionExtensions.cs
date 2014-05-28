// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.Framework.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTransient<TService, TImplementation>([NotNull]this IServiceCollection services)
        {
            return services.AddTransient(typeof(TService), typeof(TImplementation));
        }

        public static IServiceCollection AddTransient([NotNull]this IServiceCollection services, Type serviceType)
        {
            return services.AddTransient(serviceType, serviceType);
        }

        public static IServiceCollection AddTransient<TService>([NotNull]this IServiceCollection services)
        {
            return services.AddTransient(typeof(TService));
        }

        public static IServiceCollection AddScoped<TService, TImplementation>([NotNull]this IServiceCollection services)
        {
            return services.AddScoped(typeof(TService), typeof(TImplementation));
        }

        public static IServiceCollection AddScoped([NotNull]this IServiceCollection services, Type serviceType)
        {
            return services.AddScoped(serviceType, serviceType);
        }

        public static IServiceCollection AddScoped<TService>([NotNull]this IServiceCollection services)
        {
            return services.AddScoped(typeof(TService));
        }

        public static IServiceCollection AddSingleton<TService, TImplementation>([NotNull]this IServiceCollection services)
        {
            return services.AddSingleton(typeof(TService), typeof(TImplementation));
        }

        public static IServiceCollection AddSingleton([NotNull]this IServiceCollection services, Type serviceType)
        {
            return services.AddSingleton(serviceType, serviceType);
        }

        public static IServiceCollection AddSingleton<TService>([NotNull]this IServiceCollection services)
        {
            return services.AddSingleton(typeof(TService));
        }

        public static IServiceCollection AddInstance<TService>([NotNull]this IServiceCollection services, TService implementationInstance)
        {
            return services.AddInstance(typeof(TService), implementationInstance);
        }
    }
}
