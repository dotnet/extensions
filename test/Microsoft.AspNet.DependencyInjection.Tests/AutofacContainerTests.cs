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
            return CreateContainer(new FakeFallbackServiceProvider());
        }

        protected override IServiceProvider CreateContainer(IServiceProvider fallbackProvider)
        {
            var builder = new ContainerBuilder();

            AutofacRegistration.Populate(
                builder,
                TestServices.DefaultServices(),
                fallbackProvider);

            IContainer container = builder.Build();
            return container.Resolve<IServiceProvider>();
        }
    }
}