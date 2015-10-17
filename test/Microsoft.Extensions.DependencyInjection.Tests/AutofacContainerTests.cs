// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Specification;

namespace Microsoft.Extensions.DependencyInjection.Tests
{
    public class AutofacContainerTests : DependencyInjectionSpecificationTests
    {
        protected override IServiceProvider CreateServiceProvider(IServiceCollection collection)
        {
            var builder = new ContainerBuilder();
            builder.Populate(collection);

            var container = builder.Build();
            return container.Resolve<IServiceProvider>();
        }
    }
}
