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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Microsoft.Framework.DependencyInjection
{
    /// <summary>
    /// Helper code for the various activator services.
    /// </summary>
    public static class ActivatorUtilities
    {
        /// <summary>
        /// Retrieve an instance of the given type from the service provider. If one is not found then instantiate it directly.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object GetServiceOrCreateInstance(IServiceProvider services, Type type)
        {
            return GetServiceNoExceptions(services, type) ?? CreateInstance(services, type);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "IServiceProvider may throw unknown exceptions")]
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

        /// <summary>
        /// Instantiate an object of the given type, using constructor service injection if possible.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object CreateInstance(IServiceProvider services, Type type)
        {
            return CreateFactory(type).Invoke(services);
        }

        /// <summary>
        /// Instantiate an object of the given type, using constructor service injection if possible.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static T CreateInstance<T>(IServiceProvider services)
        {
            return (T)CreateInstance(services, typeof(T));
        }

        /// <summary>
        /// Creates a factory to instantiate a type using constructor service injection if possible.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Func<IServiceProvider, object> CreateFactory(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            ConstructorInfo[] constructors = type.GetTypeInfo()
                .DeclaredConstructors
                .Where(IsInjectable)
                .ToArray();

            if (constructors.Length == 1)
            {
                ParameterInfo[] parameters = constructors[0].GetParameters();
                return services =>
                {
                    var args = new object[parameters.Length];
                    for (int index = 0; index != parameters.Length; ++index)
                    {
                        args[index] = services.GetService(parameters[index].ParameterType);
                    }
                    return Activator.CreateInstance(type, args);
                };
            }
            return _ => Activator.CreateInstance(type);
        }

        private static bool IsInjectable(ConstructorInfo constructor)
        {
            return constructor.IsPublic && constructor.GetParameters().Length != 0;
        }
    }
}
