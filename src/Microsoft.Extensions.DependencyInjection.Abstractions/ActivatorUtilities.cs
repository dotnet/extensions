// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection.Abstractions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Helper code for the various activator services.
    /// </summary>
    public static class ActivatorUtilities
    {
        private static readonly MethodInfo GetServiceInfo =
            GetMethodInfo<Func<IServiceProvider, Type, Type, bool, object>>((sp, t, r, c) => GetService(sp, t, r, c));

        /// <summary>
        /// Instantiate a type with constructor arguments provided directly and/or from an <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="provider">The service provider used to resolve dependencies</param>
        /// <param name="instanceType">The type to activate</param>
        /// <param name="parameters">Constructor arguments not provided by the <see cref="provider"/></param>
        /// <returns>An activated object of type instanceType</returns>
        public static object CreateInstance(IServiceProvider provider, Type instanceType, params object[] parameters)
        {
            int bestLength = -1;
            ConstructorMatcher bestMatcher = null;

            foreach (var matcher in instanceType
                .GetTypeInfo()
                .DeclaredConstructors
                .Where(c => !c.IsStatic && c.IsPublic)
                .Select(constructor => new ConstructorMatcher(constructor)))
            {
                var length = matcher.Match(parameters);
                if (length == -1)
                {
                    continue;
                }
                if (bestLength < length)
                {
                    bestLength = length;
                    bestMatcher = matcher;
                }
            }

            if (bestMatcher == null)
            {
                throw new InvalidOperationException(Resources.FormatNoConstructorMatch(instanceType));
            }

            return bestMatcher.CreateInstance(provider);
        }

        /// <summary>
        /// Create a delegate that will instantiate a type with constructor arguments provided directly
        /// and/or from an <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="instanceType">The type to activate</param>
        /// <param name="argumentTypes">
        /// The types of objects, in order, that will be passed to the returned function as its second parameter
        /// </param>
        /// <returns>
        /// A factory that will instantiate instanceType using an <see cref="IServiceProvider"/>
        /// and an argument array containing objects matching the types defined in argumentTypes
        /// </returns>
        public static ObjectFactory CreateFactory(Type instanceType, Type[] argumentTypes)
        {
            ConstructorInfo constructor;
            int?[] parameterMap;

            FindApplicableConstructor(instanceType, argumentTypes, out constructor, out parameterMap);

            var provider = Expression.Parameter(typeof(IServiceProvider), "provider");
            var argumentArray = Expression.Parameter(typeof(object[]), "argumentArray");
            var factoryExpressionBody = BuildFactoryExpression(constructor, parameterMap, provider, argumentArray);

            var factoryLamda = Expression.Lambda<Func<IServiceProvider, object[], object>>(
                factoryExpressionBody, provider, argumentArray);

            var result = factoryLamda.Compile();
            return result.Invoke;
        }

        /// <summary>
        /// Instantiate a type with constructor arguments provided directly and/or from an <see cref="IServiceProvider"/>.
        /// </summary>
        /// <typeparam name="T">The type to activate</typeparam>
        /// <param name="provider">The service provider used to resolve dependencies</param>
        /// <param name="parameters">Constructor arguments not provided by the <see cref="provider"/></param>
        /// <returns>An activated object of type T</returns>
        public static T CreateInstance<T>(IServiceProvider provider, params object[] parameters)
        {
            return (T)CreateInstance(provider, typeof(T), parameters);
        }

        /// <summary>
        /// Retrieve an instance of the given type from the service provider. If one is not found then instantiate it directly.
        /// </summary>
        /// <typeparam name="T">The type of the service</typeparam>
        /// <param name="provider">The service provider used to resolve dependencies</param>
        /// <returns>The resolved service or created instance</returns>
        public static T GetServiceOrCreateInstance<T>(IServiceProvider provider)
        {
            return (T)GetServiceOrCreateInstance(provider, typeof(T));
        }

        /// <summary>
        /// Retrieve an instance of the given type from the service provider. If one is not found then instantiate it directly.
        /// </summary>
        /// <param name="activator">The type activator</param>
        /// <param name="type">The type of the service</param>
        /// <returns>The resolved service or created instance</returns>
        public static object GetServiceOrCreateInstance(IServiceProvider provider, Type type)
        {
            return provider.GetService(type) ?? CreateInstance(provider, type);
        }

        private static MethodInfo GetMethodInfo<T>(Expression<T> expr)
        {
            var mc = (MethodCallExpression)expr.Body;
            return mc.Method;
        }

        private static object GetService(IServiceProvider sp, Type type, Type requiredBy, bool isDefaultParameterRequired)
        {
            var service = sp.GetService(type);
            if (service == null && !isDefaultParameterRequired)
            {
                throw new InvalidOperationException(
                Resources.FormatCannotResolveService(type, requiredBy));
            }
            return service;
        }

        private static Expression BuildFactoryExpression(
            ConstructorInfo constructor,
            int?[] parameterMap,
            Expression serviceProvider,
            Expression factoryArgumentArray)
        {
            var constructorParameters = constructor.GetParameters();
            var constructorArguments = new Expression[constructorParameters.Length];

            for (var i = 0; i < constructorParameters.Length; i++)
            {
                var parameterType = constructorParameters[i].ParameterType;

                if (parameterMap[i] != null)
                {
                    constructorArguments[i] = Expression.ArrayAccess(factoryArgumentArray, Expression.Constant(parameterMap[i]));
                }
                else
                {
                    var constructorParameterHasDefault = constructorParameters[i].HasDefaultValue;
                    var parameterTypeExpression = new Expression[] { serviceProvider,
                        Expression.Constant(parameterType, typeof(Type)),
                        Expression.Constant(constructor.DeclaringType, typeof(Type)),
                        Expression.Constant(constructorParameterHasDefault) };
                    constructorArguments[i] = Expression.Call(GetServiceInfo, parameterTypeExpression);
                }

                // Support optional constructor arguments by passing in the default value
                // when the argument would otherwise be null.
                if (constructorParameters[i].HasDefaultValue)
                {
                    var defaultValueExpression = Expression.Constant(constructorParameters[i].DefaultValue);
                    constructorArguments[i] = Expression.Coalesce(constructorArguments[i], defaultValueExpression);
                }

                constructorArguments[i] = Expression.Convert(constructorArguments[i], parameterType);
            }

            return Expression.New(constructor, constructorArguments);
        }

        private static void FindApplicableConstructor(
            Type instanceType,
            Type[] argumentTypes,
            out ConstructorInfo matchingConstructor,
            out int?[] parameterMap)
        {
            matchingConstructor = null;
            parameterMap = null;

            foreach (var constructor in instanceType.GetTypeInfo().DeclaredConstructors)
            {
                if (constructor.IsStatic || !constructor.IsPublic)
                {
                    continue;
                }

                int?[] tempParameterMap;
                if (TryCreateParameterMap(constructor.GetParameters(), argumentTypes, out tempParameterMap))
                {
                    if (matchingConstructor != null)
                    {
                        throw new InvalidOperationException(Resources.FormatAmbiguousConstructorMatch(instanceType));
                    }

                    matchingConstructor = constructor;
                    parameterMap = tempParameterMap;
                }
            }

            if (matchingConstructor == null)
            {
                throw new InvalidOperationException(Resources.FormatNoConstructorMatch(instanceType));
            }
        }

        // Creates an injective parameterMap from givenParameterTypes to assignable constructorParameters.
        // Returns true if each given parameter type is assignable to a unique; otherwise, false.
        private static bool TryCreateParameterMap(ParameterInfo[] constructorParameters, Type[] argumentTypes, out int?[] parameterMap)
        {
            parameterMap = new int?[constructorParameters.Length];

            for (var i = 0; i < argumentTypes.Length; i++)
            {
                var foundMatch = false;
                var givenParameter = argumentTypes[i].GetTypeInfo();

                for (var j = 0; j < constructorParameters.Length; j++)
                {
                    if (parameterMap[j] != null)
                    {
                        // This ctor parameter has already been matched
                        continue;
                    }

                    if (constructorParameters[j].ParameterType.GetTypeInfo().IsAssignableFrom(givenParameter))
                    {
                        foundMatch = true;
                        parameterMap[j] = i;
                        break;
                    }
                }

                if (!foundMatch)
                {
                    return false;
                }
            }

            return true;
        }

        private class ConstructorMatcher
        {
            private readonly ConstructorInfo _constructor;
            private readonly ParameterInfo[] _parameters;
            private readonly object[] _parameterValues;
            private readonly bool[] _parameterValuesSet;

            public ConstructorMatcher(ConstructorInfo constructor)
            {
                _constructor = constructor;
                _parameters = _constructor.GetParameters();
                _parameterValuesSet = new bool[_parameters.Length];
                _parameterValues = new object[_parameters.Length];
            }

            public int Match(object[] givenParameters)
            {

                var applyIndexStart = 0;
                var applyExactLength = 0;
                for (var givenIndex = 0; givenIndex < givenParameters.Length; givenIndex++)
                {
                    var givenType = givenParameters[givenIndex] == null ? null : givenParameters[givenIndex].GetType().GetTypeInfo();
                    var givenMatched = false;

                    for (var applyIndex = applyIndexStart; applyIndex < _parameters.Length; applyIndex++)
                    {
                        if (_parameterValuesSet[applyIndex] == false &&
                            _parameters[applyIndex].ParameterType.GetTypeInfo().IsAssignableFrom(givenType))
                        {
                            givenMatched = true;
                            _parameterValuesSet[applyIndex] = true;
                            _parameterValues[applyIndex] = givenParameters[givenIndex];
                            if (applyIndexStart == applyIndex)
                            {
                                applyIndexStart++;
                                if (applyIndex == givenIndex)
                                {
                                    applyExactLength = applyIndex;
                                }
                            }
                            break;
                        }
                    }

                    if (givenMatched == false)
                    {
                        return -1;
                    }
                }
                return applyExactLength;
            }

            public object CreateInstance(IServiceProvider provider)
            {
                for (var index = 0; index < _parameters.Length; index++)
                {
                    if (_parameterValuesSet[index] == false)
                    {
                        var value = provider.GetService(_parameters[index].ParameterType);
                        if (value == null)
                        {
                            if (!_parameters[index].HasDefaultValue)
                            {
                                throw new InvalidOperationException(Resources.FormatCannotResolveService(
                                    _parameters[index].ParameterType,
                                    _constructor.DeclaringType));
                            }
                            else
                            {
                                _parameterValues[index] = _parameters[index].DefaultValue;
                            }
                        }
                        else
                        {
                            _parameterValues[index] = value;
                        }
                    }
                }

                try
                {
                    return _constructor.Invoke(_parameterValues);
                }
                catch (Exception ex)
                {
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                    // The above line will always throw, but the compiler requires we throw explicitly.
                    throw;
                }
            }
        }
    }
}
