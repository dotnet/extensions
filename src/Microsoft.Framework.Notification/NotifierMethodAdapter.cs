// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Microsoft.Framework.Notification
{
    public class NotifierMethodAdapter : INotifierMethodAdapter
    {
        public Func<object, object, bool> Adapt(MethodInfo method, Type inputType)
        {
#if NET45 || DNX451 || DNXCORE50
            return Internal.ProxyMethodEmitter.CreateProxyMethod(method, inputType);
#else
            return CreateReflectionAdapter(method, inputType);
#endif
        }

#if !NET45 && !DNX451 && !DNXCORE50
        private Func<object, object, bool> CreateReflectionAdapter(MethodInfo method, Type inputType)
        {
            var parameters = method.GetParameters();

            var mappings = new PropertyInfo[parameters.Length];

            var inputTypeInfo = inputType.GetTypeInfo();
            for (var i = 0; i < parameters.Length; i++)
            {
                var property = inputTypeInfo.GetDeclaredProperty(parameters[i].Name);
                if (property == null)
                {
                    continue;
                }
                else
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
                    else if (parameter.ParameterType.IsAssignableFrom(mapping.PropertyType))
                    {
                        arguments[j] = mapping.GetValue(input);
                    }
                }

                method.Invoke(instance, arguments);
                return true;
            };
        }
#endif
    }
}
