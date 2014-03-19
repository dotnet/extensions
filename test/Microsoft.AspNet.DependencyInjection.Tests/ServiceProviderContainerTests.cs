using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.DependencyInjection.Fallback;
using Microsoft.AspNet.DependencyInjection.Tests.Fakes;
using Xunit;

namespace Microsoft.AspNet.DependencyInjection.Tests
{
    public class ServiceProviderContainerTests : ScopingContainerTestBase
    {
        protected override IServiceProvider CreateContainer()
        {
            return new ServiceCollection()
                .Add(TestServices.DefaultServices())
                .BuildServiceProvider(new FakeFallbackServiceProvider());
        }
    }
}
