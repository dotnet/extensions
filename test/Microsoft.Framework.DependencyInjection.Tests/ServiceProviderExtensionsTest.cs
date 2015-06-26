// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.Framework.DependencyInjection
{
    public class ServiceProviderExtensionsTest
    {
        [Fact]
        public void GetRequiredServices_Returns_AllServices()
        {
            // Arrange
            var serviceProvider = CreateTestServiceProvider(2);

            // Act
            var services = serviceProvider.GetRequiredServices<IFoo>();

            // Assert
            Assert.Contains(services, item => item is Foo1);
            Assert.Contains(services, item => item is Foo2);
            Assert.Equal(2, services.Count());
        }

        [Fact]
        public void NonGeneric_GetRequiredServices_Returns_AllServices()
        {
            // Arrange
            var serviceProvider = CreateTestServiceProvider(2);

            // Act
            var services = serviceProvider.GetRequiredServices(typeof(IFoo));

            // Assert
            Assert.Contains(services, item => item is Foo1);
            Assert.Contains(services, item => item is Foo2);
            Assert.Equal(2, services.Count());
        }

        [Fact]
        public void GetRequiredServices_Returns_SingleService()
        {
            // Arrange
            var serviceProvider = CreateTestServiceProvider(1);

            // Act
            var services = serviceProvider.GetRequiredServices<IFoo>();

            // Assert
            var item = Assert.Single(services);
            Assert.IsType<Foo1>(item);
        }

        [Fact]
        public void NonGeneric_GetRequiredServices_Returns_SingleService()
        {
            // Arrange
            var serviceProvider = CreateTestServiceProvider(1);

            // Act
            var services = serviceProvider.GetRequiredServices(typeof(IFoo));

            // Assert
            var item = Assert.Single(services);
            Assert.IsType<Foo1>(item);
        }

        [Fact]
        public void GetRequiredServices_Returns_CorrectTypes()
        {
            // Arrange
            var serviceProvider = CreateTestServiceProvider(4);

            // Act
            var services = serviceProvider.GetRequiredServices(typeof(IAnotherFoo));

            // Assert
            foreach (var service in services)
            {
                Assert.IsAssignableFrom<IAnotherFoo>(service);
            }
            Assert.Equal(2, services.Count());
        }

        [Fact]
        public void Throws_WhenNo_Services_AreRegistered()
        {
            // Arrange
            var serviceProvider = CreateTestServiceProvider(0);

            // Act + Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => serviceProvider.GetRequiredServices<IFoo>(),
                $"No service for type '{typeof(IFoo)}' has been registered.");
        }

        [Fact]
        public void Throws_WhenNo_Services_AreRegistered_NonGeneric()
        {
            // Arrange
            var serviceProvider = CreateTestServiceProvider(0);

            // Act + Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => serviceProvider.GetRequiredServices(typeof(IFoo)),
                $"No service for type '{typeof(IFoo)}' has been registered.");
        }

        [Fact]
        public void Throws_WhenNo_Services_AreAvailable()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddInstance<IEnumerable<IFoo>>(new List<IFoo>());

            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Act + Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => serviceProvider.GetRequiredServices<IFoo>(),
                $"No service for type '{typeof(IFoo)}' has been registered.");
        }

        [Fact]
        public void Throws_WhenNo_Services_AreAvailable_NonGeneric()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddInstance<IEnumerable<IFoo>>(new List<IFoo>());

            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Act + Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => serviceProvider.GetRequiredServices(typeof(IFoo)),
                $"No service for type '{typeof(IFoo)}' has been registered.");
        }

        private IServiceProvider CreateTestServiceProvider(int count)
        {
            var serviceCollection = new ServiceCollection();

            if (count > 0)
            {
                serviceCollection.AddTransient<IFoo, Foo1>();
            }

            if (count > 1)
            {
                serviceCollection.AddTransient<IFoo, Foo2>();
            }

            if (count > 2)
            {
                serviceCollection.AddTransient<IAnotherFoo, AnotherFoo1>();
            }

            if (count > 3)
            {
                serviceCollection.AddTransient<IAnotherFoo, AnotherFoo2>();
            }

            return serviceCollection.BuildServiceProvider();
        }

        public interface IFoo { }

        public class Foo1 : IFoo { }

        public class Foo2 : IFoo { }

        public interface IAnotherFoo { }

        public class AnotherFoo1 : IAnotherFoo { }

        public class AnotherFoo2 : IAnotherFoo { }
    }
}