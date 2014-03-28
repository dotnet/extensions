using System;
using Microsoft.AspNet.DependencyInjection.Ninject;
using Microsoft.AspNet.DependencyInjection.Tests.Fakes;
using Ninject;
using Xunit;

namespace Microsoft.AspNet.DependencyInjection.Tests
{
    public class NinjectContainerTests : ScopingContainerTestBase
    {
        protected override IServiceProvider CreateContainer()
        {
            IKernel kernel = new StandardKernel();

            NinjectRegistration.Populate(
                kernel,
                TestServices.DefaultServices(),
                new FakeFallbackServiceProvider());

            return kernel.Get<IServiceProvider>();
        }

        [Fact]
        public void NestedScopedServiceCanBeResolvedFromFallbackProvider()
        {
            var container = CreateContainer();

            var outerScopeFactory = container.GetService<IServiceScopeFactory>();
            using (var outerScope = outerScopeFactory.CreateScope())
            {
                var innerScopeFactory = outerScope.ServiceProvider.GetService<IServiceScopeFactory>();
                using (var innerScope = innerScopeFactory.CreateScope())
                {
                    var outerScopedService = outerScope.ServiceProvider.GetService<string>();
                    var innerScopedService = innerScope.ServiceProvider.GetService<string>();

                    Assert.Equal("scope-FakeFallbackServiceProvider", outerScopedService);
                    Assert.Equal("scope-scope-FakeFallbackServiceProvider", innerScopedService);
                }
            }
        }
    }
}