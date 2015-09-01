// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// We only need ReflectionNotifierMethodAdapter for netcore50 or any case where we can't use Reflection.Emit.
//
// However to test it, we compile it into each test assembly. This lets us write tests without a separate
// flavor of tests for netcore50.
#if !PROXY_SUPPORT || TEST

using System;
using System.Reflection;

namespace Microsoft.Framework.TelemetryAdapter
{
    public class ReflectionTelemetrySourceMethodAdapter : ITelemetrySourceMethodAdapter
    {
        public Func<object, object, bool> Adapt(MethodInfo method, Type inputType)
        {
            return CreateReflectionAdapter(method, inputType);
        }

        private static Func<object, object, bool> CreateReflectionAdapter(MethodInfo method, Type inputType)
        {
            var parameters = method.GetParameters();

            var mappings = new PropertyInfo[parameters.Length];

            var inputTypeInfo = inputType.GetTypeInfo();
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var property = inputTypeInfo.GetDeclaredProperty(parameter.Name);
                if (property == null)
                {
                    continue;
                }
                else if (parameter.ParameterType.IsAssignableFrom(property.PropertyType))
                {
                    mappings[i] = property;
                }
            }

            return (instance, input) =>
            {
                if (input.GetType() != inputType)
                {
                    return false;
                }

                var arguments = new object[mappings.Length];
                for (var j = 0; j < mappings.Length; j++)
                {
                    var mapping = mappings[j];
                    var parameter = parameters[j];
                    if (mapping == null)
                    {
                        if (parameter.ParameterType.GetTypeInfo().IsValueType)
                        {
                            arguments[j] = Activator.CreateInstance(parameter.ParameterType);
                        }
                    }
                    else
                    {
                        arguments[j] = mapping.GetValue(input);
                    }
                }

                method.Invoke(instance, arguments);
                return true;
            };
        }
   }
}

#endif
