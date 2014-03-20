using System;
using Autofac;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.DependencyInjection.Autofac;
using Microsoft.AspNet.DependencyInjection.Tests.Fakes;
using Xunit;

namespace Microsoft.AspNet.DependencyInjection.Tests
{
    public class AutofacContainerTests : ScopingContainerTestBase
    {
        protected override IServiceProvider CreateContainer()
        {
            var builder = new ContainerBuilder();

            AutofacRegistration.Populate(
                builder,
                new FakeFallbackServiceProvider(),
                TestServices.DefaultServices());

            IContainer container = builder.Build();
            return container.Resolve<IServiceProvider>();
        }

        [Fact]
        public void OpenGenericServicesCanBeRegisetered()
        {
            var container = CreateContainer();

            var genericService = container.GetService<IFakeOpenGenericService<IFakeSingletonService>>();
            var singletonService = container.GetService<IFakeSingletonService>();

            Assert.Equal(singletonService, genericService.SimpleMethod());
        }

        [Fact]
        public void ClosedServicesPreferredOverOpenGenericServices()
        {
            var container = CreateContainer();

            var service = container.GetService<IFakeOpenGenericService<string>>();

            Assert.Equal("FakeServiceSimpleMethod", service.SimpleMethod());
        }
    }
}