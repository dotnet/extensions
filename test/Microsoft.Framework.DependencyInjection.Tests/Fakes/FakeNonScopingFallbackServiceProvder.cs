// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.DependencyInjection.Tests.Fakes
{
    public class FakeNonScopingFallbackServiceProvder : IServiceProvider
    {
        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(string))
            {
                return "FakeNonScopingFallbackServiceProvder";
            }
            else
            {
                throw new Exception();
            }
        }
    }
}
