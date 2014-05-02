// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNet.DependencyInjection
{
    public static class ServiceProviderExtensions
    {
        /// <summary>
        /// Retrieve a service of type T from the IServiceProvider.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static T GetService<T>(this IServiceProvider services)
        {
            if (services == null)
            {
                throw new ArgumentNullException("services");
            }

            return (T)services.GetService(typeof(T));
        }

        /// <summary>
        /// Retrieve a service of type T from the IServiceProvider.
        /// Return T's default value if no service of type T is available.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "IServiceProvider may throw unknown exceptions")]
        public static T GetServiceOrDefault<T>(this IServiceProvider services)
        {
            try
            {
                return services.GetService<T>();
            }
            catch (Exception)
            {
                return default(T);
            }
        }

        public static bool HasService<TService>(this IServiceProvider provider)
        {
            return provider.HasService(typeof(TService));
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "IServiceProvider may throw unknown exceptions")]
        public static bool HasService(this IServiceProvider provider, Type serviceType)
        {
            try
            {
                var obj = provider.GetService(serviceType);

                // Return false for empty enumerables
                if (obj is IEnumerable)
                {
                    return ((IEnumerable)obj).GetEnumerator().MoveNext();
                }

                return obj != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
