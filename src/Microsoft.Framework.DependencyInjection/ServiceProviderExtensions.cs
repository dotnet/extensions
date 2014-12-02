// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.DependencyInjection
{
    public static class ServiceProviderExtensions
    {
        /// <summary>
        /// Get service of type <typeparamref name="T"/> from the IServiceProvider.
        /// </summary>
        /// <typeparam name="T">The type of service object to get.</typeparam>
        /// <param name="provider">The <see cref="IServiceProvider"/> to retrieve the service object from.</param>
        /// <returns>A service object of type <typeparamref name="T"/> or null if there is no such service.</returns>
        public static T GetService<T>([NotNull] this IServiceProvider provider)
        {
            return (T)provider.GetService(typeof(T));
        }

        /// <summary>
        /// Get service of type <paramref name="serviceType"/> from the IServiceProvider.
        /// </summary>
        /// <param name="provider">The <see cref="IServiceProvider"/> to retrieve the service object from.</param>
        /// <param name="serviceType">An object that specifies the type of service object to get.</param>
        /// <returns>A service object of type <paramref name="serviceType"/>.</returns>
        /// <exception cref="System.Exception">There is no service of type <paramref name="serviceType"/>.</exception>
        public static object GetRequiredService([NotNull] this IServiceProvider provider, [NotNull] Type serviceType)
        {
            var service = provider.GetService(serviceType);

            if (service == null)
            {
                throw new Exception(Resources.FormatNoServiceRegistered(serviceType));
            }

            return service;
        }

        /// <summary>
        /// Get service of type <typeparamref name="T"/> from the IServiceProvider.
        /// </summary>
        /// <typeparam name="T">The type of service object to get.</typeparam>
        /// <param name="provider">The <see cref="IServiceProvider"/> to retrieve the service object from.</param>
        /// <returns>A service object of type <typeparamref name="T"/>.</returns>
        /// <exception cref="System.Exception">There is no service of type <typeparamref name="T"/>.</exception>
        public static T GetRequiredService<T>([NotNull] this IServiceProvider provider)
        {
            return (T)provider.GetRequiredService(typeof(T));
        }
    }
}
