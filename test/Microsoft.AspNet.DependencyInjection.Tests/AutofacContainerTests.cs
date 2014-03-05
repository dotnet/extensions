using System;
using Autofac;
using Microsoft.AspNet.DependencyInjection.Autofac;
using Microsoft.AspNet.DependencyInjection.Tests.Fakes;
using Xunit;

namespace Microsoft.AspNet.DependencyInjection.Tests
{
    public class AutofacContainerTests : AllContainerTestsBase
    {
        protected override IServiceProvider CreateContainer()
        {
            var builder = new ContainerBuilder();

            AutofacRegistration.Populate(builder, TestServices.DefaultServices());

            IContainer container = builder.Build();
            return container.Resolve<IServiceProvider>();
        }

        [Fact]
        public void SingletonServiceCanBeResolved()
        {
            var container = CreateContainer();

            var service1 = container.GetService<IFakeSingletonService>();
            var service2 = container.GetService<IFakeSingletonService>();

            Assert.NotNull(service1);
            Assert.Equal(service1, service2);
        }

        public class AutofacServiceProvider : IServiceProvider
        {
            private readonly IComponentContext _componentContext;

            public AutofacServiceProvider(IComponentContext componentContext)
            {
                _componentContext = componentContext;
            }

            public object GetService(Type serviceType)
            {
                return _componentContext.Resolve(serviceType);
            }
        }
    }
}