// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Castle.Facilities.AspNetCore;
using Castle.Windsor;
using Microsoft.Extensions.DependencyInjection.Specification;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection.Specification
{
    public class WindsorDependencyInjectionSpecificationTests : SkippableDependencyInjectionSpecificationTests
    {
        private class HomeController
        { }
        public override string[] SkippedTests => new[]
        {
            "ResolvesDifferentInstancesForServiceWhenResolvingEnumerable"
        };


        protected override IServiceProvider CreateServiceProviderImpl(IServiceCollection serviceCollection)
        {
            var container = new WindsorContainer();
            // Setup component model contributors for making windsor services available to IServiceProvider
            container.AddFacility<AspNetCoreFacility>(f => f.CrossWiresInto(serviceCollection));

            // Add framework services.
            // serviceCollection.AddMVC();
            // ...

            // Custom application component registrations, ordering is important here
            //RegisterApplicationComponents(serviceCollection);


            // Castle Windsor integration, controllers, tag helpers and view components, this should always come after RegisterApplicationComponents
            return serviceCollection.AddWindsor(container,
                opts => opts.UseEntryAssembly(typeof(HomeController).Assembly), // <- Recommended
                () => serviceCollection.BuildServiceProvider(validateScopes: false)); // <- Optional        }
        }
    }
}
