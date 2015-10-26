// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.DiagnosticAdapter.Infrastructure;
using Xunit;

namespace Microsoft.Extensions.DiagnosticAdapter.Internal
{
    public class ProxyFactoryTest
    {
        [Fact]
        public void CreateProxy_Null()
        {
            // Arrange
            var factory = new ProxyFactory();

            // Act
            var result = factory.CreateProxy<IPerson>(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void CreateProxy_ValueType_Null()
        {
            // Arrange
            var factory = new ProxyFactory();

            // Act
            var result = factory.CreateProxy<int>(null);

            // Assert
            Assert.Equal(default(int), result);
        }

        [Fact]
        public void CreateProxy_Assignable()
        {
            // Arrange
            var factory = new ProxyFactory();
            var value = new Person() { Name = "Joey" };

            // Act
            var result = factory.CreateProxy<Person>(value);

            // Assert
            Assert.Same(value, result);
        }

        [Fact]
        public void CreateProxy_Proxy()
        {
            // Arrange
            var factory = new ProxyFactory();
            var value = new Person() { Name = "Joey" };

            // Act
            var result = factory.CreateProxy<IPerson>(value);

            // Assert
            Assert.Same(value.Name, result.Name);
        }

        [Fact]
        public void CreateProxy_Proxy_CanUnwrap()
        {
            // Arrange
            var factory = new ProxyFactory();
            var value = new Person() { Name = "Joey" };

            // Act
            var result = (IProxy)factory.CreateProxy<IPerson>(value);

            // Assert
            Assert.Same(value, result.Upwrap<Person>());
        }

        public class Person
        {
            public string Name { get; set; }
        }

        public interface IPerson
        {
            string Name { get; }
        }
    }
}
