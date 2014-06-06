// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.Framework.DependencyInjection.Tests.Fakes
{
    public class FakeNonScopingFallbackServiceProvder : IServiceProvider
    {
        public object GetService(Type serviceType)
        {
            var typeInfo = serviceType.GetTypeInfo();

            if (serviceType == typeof(string))
            {
                return "FakeNonScopingFallbackServiceProvder";
            }
            if (serviceType == typeof(IEnumerable<string>))
            {
                return new[] { "FakeNonScopingFallbackServiceProvder" };
            }
            if (typeInfo.IsGenericType &&
                typeInfo.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var innerType = typeInfo.GenericTypeArguments.Single();
                return Array.CreateInstance(innerType, 0);
            }
            else
            {
                throw new Exception();
            }
        }
    }
}
