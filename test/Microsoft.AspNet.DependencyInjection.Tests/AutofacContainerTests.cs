using System;
using Autofac;
using Microsoft.AspNet.DependencyInjection.Autofac;
using Microsoft.AspNet.DependencyInjection.Tests.Fakes;

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