// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Microsoft.Framework.DependencyInjection
{
    public class TypeActivator : ITypeActivator
    {
        private static readonly MethodInfo GetServiceInfo = GetMethodInfo<Func<IServiceProvider, Type, object>>((sp, t) => sp.GetService(t));

        public object CreateInstance(IServiceProvider services, Type instanceType, params object[] parameters)
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

            return bestMatcher.CreateInstance(services);
        }

        public Func<IServiceProvider, object[], object> CreateFactory(Type instanceType, Type[] argumentTypes)
        {
            ConstructorInfo constructor;
            int?[] parameterMap;

            FindApplicableConstructor(instanceType, argumentTypes, out constructor, out parameterMap);

            var serviceProvider = Expression.Parameter(typeof(IServiceProvider), "provider");
            var argumentArray = Expression.Parameter(typeof(object[]), "argumentArray");
            var factoryExpressionBody = BuildFactoryExpression(constructor, parameterMap, serviceProvider, argumentArray);

            var factoryLamda = Expression.Lambda<Func<IServiceProvider, object[], object>>(
                factoryExpressionBody,
                serviceProvider, argumentArray);

            return factoryLamda.Compile();
        }

        private static MethodInfo GetMethodInfo<T>(Expression<T> expr)
        {
            var mc = (MethodCallExpression)expr.Body;
            return mc.Method;
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
                    var parameterTypeExpression = Expression.Constant(parameterType);
                    constructorArguments[i] = Expression.Call(serviceProvider, GetServiceInfo, parameterTypeExpression);
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

        class ConstructorMatcher
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
                for (var givenIndex = 0; givenIndex != givenParameters.Length; ++givenIndex)
                {
                    var givenType = givenParameters[givenIndex] == null ? null : givenParameters[givenIndex].GetType().GetTypeInfo();
                    var givenMatched = false;

                    for (var applyIndex = applyIndexStart; givenMatched == false && applyIndex != _parameters.Length; ++applyIndex)
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
                        }
                    }

                    if (givenMatched == false)
                    {
                        return -1;
                    }
                }
                return applyExactLength;
            }

            public object CreateInstance(IServiceProvider _services)
            {
                for (var index = 0; index != _parameters.Length; ++index)
                {
                    if (_parameterValuesSet[index] == false)
                    {
                        var value = _services.GetService(_parameters[index].ParameterType);
                        if (value == null)
                        {
                            if (!_parameters[index].HasDefaultValue)
                            {
                                throw new InvalidOperationException(Resources.FormatCannotResolveService(
                                    _constructor.DeclaringType, 
                                    _parameters[index].ParameterType));
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
