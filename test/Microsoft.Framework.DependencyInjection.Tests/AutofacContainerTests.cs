// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Autofac;
using Microsoft.Framework.DependencyInjection.Autofac;
using Microsoft.Framework.DependencyInjection.Tests.Fakes;

namespace Microsoft.Framework.DependencyInjection.Tests
{
    public class AutofacContainerTests : ScopingContainerTestBase
    {
        protected override IServiceProvider CreateContainer()
        {
            var builder = new ContainerBuilder();

            builder.Populate(TestServices.DefaultServices());

            IContainer container = builder.Build();
            return container.Resolve<IServiceProvider>();
        }
    }
}