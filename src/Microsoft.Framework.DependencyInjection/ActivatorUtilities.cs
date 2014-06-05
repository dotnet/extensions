// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "IServiceProvider may throw unknown exceptions")]
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
                        try
                        {
                            args[index] = services.GetService(parameters[index].ParameterType);
                        }
                        catch (Exception innerException)
                        {
                            throw new Exception(
                                string.Format("TODO: Unable to resolve service for type '{0}' while attempting to activate '{1}'.",
                                    parameters[index].ParameterType, type),
                                innerException);
                        }
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
