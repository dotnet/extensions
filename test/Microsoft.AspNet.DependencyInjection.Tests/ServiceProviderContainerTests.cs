using System;
using Microsoft.AspNet.DependencyInjection.Fallback;
using Microsoft.AspNet.DependencyInjection.Tests.Fakes;

namespace Microsoft.AspNet.DependencyInjection.Tests
{
    public class ServiceProviderContainerTests : ScopingContainerTestBase
    {
        protected override IServiceProvider CreateContainer()
        {
            return TestServices.DefaultServices()
                .BuildServiceProvider(new FakeFallbackServiceProvider());
        }
    }
}
