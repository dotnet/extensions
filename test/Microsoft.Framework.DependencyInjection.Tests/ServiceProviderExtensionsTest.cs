// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.Framework.DependencyInjection
{
    public class ServiceProviderExtensionsTest
    {
        [Fact]
        public void GetServicesReturnsAllServices()
        {
            // Arrange
            var serviceProvider = CreateFooServiceProvider(2);

            // Act
            var services = serviceProvider.GetRequiredServices<IFoo>();

            // Assert
            Assert.Contains(services, item => item is Foo1);
            Assert.Contains(services, item => item is Foo2);
            Assert.Equal(2, services.Count());
        }

        [Fact]
        public void GetServicesReturns_A_Service()
        {
            // Arrange
            var serviceProvider = CreateFooServiceProvider(1);

            // Act
            var services = serviceProvider.GetRequiredServices<IFoo>();

            // Assert
            Assert.Contains(services, item => item is Foo1);
            Assert.Equal(1, services.Count());
        }

        [Fact]
        public void ThrowsWhenNoServicesAreRegistered()
        {
            // Arrange
            var serviceProvider = CreateFooServiceProvider(0);

            // Act + Assert
            Assert.Throws<InvalidOperationException>(() => serviceProvider.GetRequiredServices<IFoo>());
        }

        [Fact]
        public void ThrowsWhenNoServicesAreAvailable()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddInstance<IEnumerable<IFoo>>(new List<IFoo>());

            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Act + Assert
            Assert.Throws<InvalidOperationException>(() => serviceProvider.GetRequiredServices<IFoo>());
        }

        private IServiceProvider CreateFooServiceProvider(int count)
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

            return serviceCollection.BuildServiceProvider();
        }

        public interface IFoo { }

        public class Foo1 : IFoo { }

        public class Foo2 : IFoo { }
    }
}