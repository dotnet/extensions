// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Framework.DependencyInjection
{
    public static class ServiceProviderExtensions
    {
        /// <summary>
        /// Retrieve a service of type T from the IServiceProvider.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static T GetService<T>(this IServiceProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }

            return (T)provider.GetService(typeof(T));
        }

        /// <summary>
        /// Retrieve a service of type T from the IServiceProvider.
        /// Return T's default value if no service of type T is available.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static T GetServiceOrDefault<T>(this IServiceProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }

            IEnumerable<T> serviceList = provider.GetAllServices<T>();
            return serviceList.FirstOrDefault();
        }

        /// <summary>
        /// Retrieve a service of type serviceType from the IServiceProvider.
        /// Return null if no service of type serviceType is available.
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public static object GetServiceOrNull(this IServiceProvider provider, Type serviceType)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }

            if (provider == null)
            {
                throw new ArgumentNullException("serviceType");
            }

            var serviceList = (IEnumerable<object>)provider.GetAllServices(serviceType);
            return serviceList.FirstOrDefault();
        }

        private static IEnumerable<T> GetAllServices<T>(this IServiceProvider provider)
        {
            return (IEnumerable<T>)provider.GetAllServices(typeof(T));
        }

        private static IEnumerable GetAllServices(this IServiceProvider provider, Type serviceType)
        {
            Type ienumerableType = typeof(IEnumerable<>).MakeGenericType(serviceType);
            return (IEnumerable)provider.GetService(ienumerableType);
        }
    }
}
