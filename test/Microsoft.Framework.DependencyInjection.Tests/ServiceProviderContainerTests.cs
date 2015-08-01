// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Framework.DependencyInjection.Tests.Fakes;
using Xunit;

namespace Microsoft.Framework.DependencyInjection.Tests
{
    public class ServiceProviderContainerTests : ScopingContainerTestBase
    {
        protected override IServiceProvider CreateContainer()
        {
            return TestServices.DefaultServices().BuildServiceProvider();
        }

        [Fact]
        public void RethrowOriginalExceptionFromConstructor()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<ClassWithThrowingEmptyCtor>();
            serviceCollection.AddTransient<ClassWithThrowingCtor>();
            serviceCollection.AddTransient<AbstractClass>();
            serviceCollection.AddTransient<IFakeService, FakeService>();

            var provider = serviceCollection.BuildServiceProvider();

            var ex1 = Assert.Throws<Exception>(() => provider.GetService<ClassWithThrowingEmptyCtor>());
            Assert.Equal(nameof(ClassWithThrowingEmptyCtor), ex1.Message);

            var ex2 = Assert.Throws<Exception>(() => provider.GetService<ClassWithThrowingCtor>());
            Assert.Equal(nameof(ClassWithThrowingCtor), ex2.Message);

            Assert.Throws<MissingMethodException>(() => provider.GetService<AbstractClass>());
        }

        [Fact]
        public void ConsumingServiceThatDependsOnServiceWithoutAnImplementationThrows()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<IServiceWithoutImplementation>();
            serviceCollection.AddTransient<DependsOnServiceWithoutImplementation>();
            var provider = serviceCollection.BuildServiceProvider();

            // Act and Assert
            var exception = Assert.Throws<MissingMethodException>(
                () => provider.GetService<DependsOnServiceWithoutImplementation>());

            Assert.Equal("Cannot create an instance of an interface.", exception.Message);
        }

        [Fact]
        public void ConsumingAServiceWithAnOpenGenericImplementationTypeThrows()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient(typeof(IList<int>), typeof(List<>));
            var provider = serviceCollection.BuildServiceProvider();

            // Act and Assert
            Assert.Throws<ArgumentException>(() => provider.GetService<IList<int>>());
        }
    }
}
