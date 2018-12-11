    // Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;

    namespace Microsoft.Extensions.DependencyInjection.Specification
{
    public class DryIocDependencyInjectionSpecificationTests: DependencyInjectionSpecificationTests
    {
        protected override IServiceProvider CreateServiceProvider(IServiceCollection serviceCollection)
        {
            return new Container()
                // setup DI adapter
                .WithDependencyInjectionAdapter(serviceCollection)
                // add registrations from CompositionRoot classs
                .BuildServiceProvider();
        }
    }
}
