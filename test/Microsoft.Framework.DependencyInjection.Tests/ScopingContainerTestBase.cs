// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.DependencyInjection.Tests.Fakes;
using Xunit;
using System.Threading;

namespace Microsoft.Framework.DependencyInjection.Tests
{
    public abstract class ScopingContainerTestBase : AllContainerTestsBase
    {
        [Fact]
        public void LastServiceReplacesPreviousServices()
        {
            var container = CreateContainer();

            var service = container.GetService<IFakeMultipleService>();

            Assert.Equal("FakeTwoMultipleServiceAnotherMethod", service.SimpleMethod());
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
            using (var scope = scopeFactory.CreateScope())
            {
                var containerScopedService = container.GetService<IFakeScopedService>();
                var scopedService1 = scope.ServiceProvider.GetService<IFakeScopedService>();
                Thread.Sleep(200);
                var scopedService2 = scope.ServiceProvider.GetService<IFakeScopedService>();

                Assert.NotEqual(containerScopedService, scopedService1);
                Assert.Equal(scopedService1, scopedService2);
            }
        }

        [Fact]
        public void NestedScopedServiceCanBeResolved()
        {
            var container = CreateContainer();

            var outerScopeFactory = container.GetService<IServiceScopeFactory>();
            using (var outerScope = outerScopeFactory.CreateScope())
            {
                var innerScopeFactory = outerScope.ServiceProvider.GetService<IServiceScopeFactory>();
                using (var innerScope = innerScopeFactory.CreateScope())
                {
                    var outerScopedService = outerScope.ServiceProvider.GetService<IFakeScopedService>();
                    var innerScopedService = innerScope.ServiceProvider.GetService<IFakeScopedService>();

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
            using (var scope = scopeFactory.CreateScope())
            {
                disposableService = (FakeService)scope.ServiceProvider.GetService<IFakeScopedService>();

                Assert.False(disposableService.Disposed);
            }

            Assert.True(disposableService.Disposed);
        }

        [Fact]
        public void SingletonServicesComeFromRootContainer()
        {
            var container = CreateContainer();
            FakeService disposableService1;
            FakeService disposableService2;

            var scopeFactory = container.GetService<IServiceScopeFactory>();
            using (var scope = scopeFactory.CreateScope())
            {
                disposableService1 = (FakeService)scope.ServiceProvider.GetService<IFakeSingletonService>();

                Assert.False(disposableService1.Disposed);
            }
            Assert.False(disposableService1.Disposed);

            using (var scope = scopeFactory.CreateScope())
            {
                disposableService2 = (FakeService)scope.ServiceProvider.GetService<IFakeSingletonService>();

                Assert.False(disposableService2.Disposed);
            }
            Assert.False(disposableService2.Disposed);

            Assert.Same(disposableService1, disposableService2);
        }

        [Fact]
        public void NestedScopedServiceCanBeResolvedWithNoFallbackProvider()
        {
            var container = CreateContainer();

            var outerScopeFactory = container.GetService<IServiceScopeFactory>();
            using (var outerScope = outerScopeFactory.CreateScope())
            {
                var innerScopeFactory = outerScope.ServiceProvider.GetService<IServiceScopeFactory>();
                using (var innerScope = innerScopeFactory.CreateScope())
                {
                    var outerScopedService = outerScope.ServiceProvider.GetService<IFakeScopedService>();
                    var innerScopedService = innerScope.ServiceProvider.GetService<IFakeScopedService>();

                    Assert.NotEqual(outerScopedService, innerScopedService);
                }
            }
        }

        [Fact]
        public void NestedScopedServiceCanBeResolvedWithNonScopingFallbackProvider()
        {
            var container = CreateContainer();

            var outerScopeFactory = container.GetService<IServiceScopeFactory>();
            using (var outerScope = outerScopeFactory.CreateScope())
            {
                var innerScopeFactory = outerScope.ServiceProvider.GetService<IServiceScopeFactory>();
                using (var innerScope = innerScopeFactory.CreateScope())
                {
                    var outerScopedService = outerScope.ServiceProvider.GetService<IFakeScopedService>();
                    var innerScopedService = innerScope.ServiceProvider.GetService<IFakeScopedService>();

                    Assert.NotEqual(outerScopedService, innerScopedService);
                }
            }
        }

        [Fact]
        public void OpenGenericServicesCanBeResolved()
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

        [Fact]
        public void AttemptingToResolveNonexistentServiceReturnsNull()
        {
            var container = CreateContainer();

            Assert.Null(container.GetService<INonexistentService>());
        }

        [Fact]
        public void AttemptingToResolveNonexistentServiceIndirectlyThrows()
        {
            var container = CreateContainer();

            Assert.ThrowsAny<Exception>(() => container.GetService<IDependOnNonexistentService>());
        }

        [Fact]
        public void NonexistentServiceCanBeIEnumerableResolved()
        {
            var container = CreateContainer();

            var services = container.GetService<IEnumerable<INonexistentService>>();

            Assert.Empty(services);
        }

        [Fact]
        public void AttemptingToIEnumerableResolveNonexistentServiceIndirectlyThrows()
        {
            var container = CreateContainer();

            // The call to ToArray is necessary for Ninject to throw
            Assert.ThrowsAny<Exception>(() =>
                container.GetService<IEnumerable<IDependOnNonexistentService>>().ToArray());
        }
    }
}
