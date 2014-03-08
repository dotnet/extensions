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

        [Fact]
        public void ScopedServiceCanBeResolved()
        {
            var container = CreateContainer();

            var scopeFactory = container.GetService<IServiceScopeFactory>();
            IServiceProvider scope;
            using (scopeFactory.CreateScope(out scope))
            {
                var containerScopedService = container.GetService<IFakeScopedService>();
                var scopedService1 = scope.GetService<IFakeScopedService>();
                var scopedService2 = scope.GetService<IFakeScopedService>();

                Assert.NotEqual(containerScopedService, scopedService1);
                Assert.Equal(scopedService1, scopedService2);
            }
        }

        [Fact]
        public void NestedScopedServiceCanBeResolved()
        {
            var container = CreateContainer();

            var outerScopeFactory = container.GetService<IServiceScopeFactory>();
            IServiceProvider outerScope;
            using (outerScopeFactory.CreateScope(out outerScope))
            {
                var innerScopeFactory = outerScope.GetService<IServiceScopeFactory>();
                IServiceProvider innerScope;
                using (innerScopeFactory.CreateScope(out innerScope))
                {
                    var outerScopedService = outerScope.GetService<IFakeScopedService>();
                    var innerScopedService = innerScope.GetService<IFakeScopedService>();

                    Assert.NotEqual(outerScopedService, innerScopedService);
                }
            }
        }

        [Fact]
        public void DisposingScopeDisposesService()
        {
            var container = CreateContainer();
            FakeService disposableService;

            var scopeFactory = container.GetService<IServiceScopeFactory>();
            IServiceProvider scope;
            using (scopeFactory.CreateScope(out scope))
            {
                disposableService = (FakeService)scope.GetService<IFakeScopedService>();

                Assert.False(disposableService.Disposed);
            }

            Assert.True(disposableService.Disposed);
        }
    }
}