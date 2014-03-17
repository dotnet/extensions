using System;
using Microsoft.AspNet.DependencyInjection.Fallback;
using Microsoft.AspNet.DependencyInjection.Tests.Fakes;

namespace Microsoft.AspNet.DependencyInjection.Tests
{
    public class ServiceProviderContainerTests : AllContainerTestsBase
    {
        protected override IServiceProvider CreateContainer()
        {
            return new ServiceCollection()
                .Add(TestServices.DefaultServices())
                .BuildServiceProvider();
        }
    }
}
