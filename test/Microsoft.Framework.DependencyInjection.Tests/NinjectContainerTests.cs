// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.DependencyInjection.Ninject;
using Microsoft.Framework.DependencyInjection.Tests.Fakes;
using Ninject;

namespace Microsoft.Framework.DependencyInjection.Tests
{
    public class NinjectContainerTests : ScopingContainerTestBase
    {
        protected override IServiceProvider CreateContainer()
        {
            IKernel kernel = new StandardKernel();

            kernel.Populate(TestServices.DefaultServices());

            return kernel.Get<IServiceProvider>();
        }
    }
}