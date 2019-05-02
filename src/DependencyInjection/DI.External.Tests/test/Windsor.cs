using Castle.Facilities.AspNetCore;
using Castle.Windsor;
using Microsoft.Extensions.DependencyInjection.Specification;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    public class WindsorDependencyInjectionSpecificationTests : SkippableDependencyInjectionSpecificationTests
    {
        private static readonly WindsorContainer Container = new WindsorContainer();
        private class HomeController
        { }
        public override string[] SkippedTests => Array.Empty<string>();


        protected override IServiceProvider CreateServiceProviderImpl(IServiceCollection serviceCollection)
        {
            // Setup component model contributors for making windsor services available to IServiceProvider
            Container.AddFacility<AspNetCoreFacility>(f => f.CrossWiresInto(serviceCollection));

            // Add framework services.
            // serviceCollection.AddMVC();
            // ...

            // Custom application component registrations, ordering is important here
            //RegisterApplicationComponents(serviceCollection);


            // Castle Windsor integration, controllers, tag helpers and view components, this should always come after RegisterApplicationComponents
            return serviceCollection.AddWindsor(Container,
                opts => opts.UseEntryAssembly(typeof(HomeController).Assembly), // <- Recommended
                () => serviceCollection.BuildServiceProvider(validateScopes: false)); // <- Optional        }
        }
    }
}
