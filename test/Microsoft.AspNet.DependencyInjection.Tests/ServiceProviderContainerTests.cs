using System;
using Microsoft.AspNet.DependencyInjection.Fallback;
using Microsoft.AspNet.DependencyInjection.Tests.Fakes;

namespace Microsoft.AspNet.DependencyInjection.Tests
{
    public class ServiceProviderContainerTests : AllContainerTestsBase
    {
        protected override IServiceProvider CreateContainer()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.Add(TestServices.DefaultServices());
            return serviceCollection.BuildServiceProvider();
        }
    }
}
