using System;
using Microsoft.AspNet.DependencyInjection.Tests.Fakes;

namespace Microsoft.AspNet.DependencyInjection.Tests
{
    public class ServiceProviderContainerTests : AllContainerTestsBase
    {
        protected override IServiceProvider CreateContainer()
        {
            return new ServiceProvider().Add(TestServices.DefaultServices());
        }
    }
}
