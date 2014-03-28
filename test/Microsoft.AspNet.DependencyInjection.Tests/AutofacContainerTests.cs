using System;
using Autofac;
using Microsoft.AspNet.DependencyInjection.Autofac;
using Microsoft.AspNet.DependencyInjection.Tests.Fakes;

namespace Microsoft.AspNet.DependencyInjection.Tests
{
    public class AutofacContainerTests : ScopingContainerTestBase
    {
        protected override IServiceProvider CreateContainer()
        {
            var builder = new ContainerBuilder();

            AutofacRegistration.Populate(
                builder,
                TestServices.DefaultServices(),
                new FakeFallbackServiceProvider());

            IContainer container = builder.Build();
            return container.Resolve<IServiceProvider>();
        }
    }
}