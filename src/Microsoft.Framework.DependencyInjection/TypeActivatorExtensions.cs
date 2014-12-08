// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.DependencyInjection
{
    public static class TypeActivatorExtensions
    {
        public static T CreateInstance<T>(this ITypeActivator activator, IServiceProvider serviceProvider, params object[] parameters)
        {
            return (T)CreateInstance(activator, serviceProvider, typeof(T), parameters);
        }

        /// <summary>
        /// Retrieve an instance of the given type from the service provider. If one is not found then instantiate it directly.
        /// </summary>
        /// <typeparam name="T">The type of the service</typeparam>
        /// <param name="activator">The type activator</param>
        /// <param name="services">The service provider used to resolve dependencies</param>
        /// <returns>The resolved service or created instance</returns>
        public static T GetServiceOrCreateInstance<T>(this ITypeActivator activator, IServiceProvider services)
        {
            return (T)GetServiceOrCreateInstance(activator, services, typeof(T));
        }

        /// <summary>
        /// Retrieve an instance of the given type from the service provider. If one is not found then instantiate it directly.
        /// </summary>
        /// <param name="activator">The type activator</param>
        /// <param name="services">The service provider used to resolve dependencies</param>
        /// <param name="type">The type of the service</param>
        /// <returns>The resolved service or created instance</returns>
        public static object GetServiceOrCreateInstance(this ITypeActivator activator, IServiceProvider services, Type type)
        {
            return GetServiceNoExceptions(services, type) ?? CreateInstance(activator, services, type);
        }

        private static object GetServiceNoExceptions(IServiceProvider services, Type type)
        {
            try
            {
                return services.GetService(type);
            }
            catch
            {
                return null;
            }
        }

        private static object CreateInstance(this ITypeActivator activator, IServiceProvider serviceProvider, Type serviceType, params object[] parameters)
        {
            if (activator == null)
            {
                throw new ArgumentNullException(nameof(activator));
            }

            return activator.CreateInstance(serviceProvider, serviceType, parameters);
        }
    }
}