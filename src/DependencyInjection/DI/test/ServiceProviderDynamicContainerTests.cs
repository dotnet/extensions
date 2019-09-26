// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit.Abstractions;

namespace Microsoft.Extensions.DependencyInjection.Tests
{
    public class ServiceProviderDynamicContainerTests : ServiceProviderContainerTests
    {
        protected override IServiceProvider CreateServiceProvider(IServiceCollection collection) =>
            collection.BuildServiceProvider();

        public ServiceProviderDynamicContainerTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }
    }
}
