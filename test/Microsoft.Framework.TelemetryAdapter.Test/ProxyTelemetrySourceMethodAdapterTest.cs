// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using System.Reflection;
using Xunit;

namespace Microsoft.Framework.TelemetryAdapter
{
    public class ProxyTelemetrySourceMethodAdapterTest
    {
        [Fact]
        public void Adapt_ReturnsTrueForTypeMatch()
        {
            // Arrange
            var adapter = new ProxyTelemetrySourceMethodAdapter();

            var listener = new Listener1();
            var method = GetMethodInfo<Listener1>(l => l.Listen());

            // Act
            var func = adapter.Adapt(method, new { }.GetType());

            // Assert
            Assert.True(func(listener, new { }));
        }

        [Fact]
        public void Adapt_ReturnsFalseForTypeNotMatching()
        {
            // Arrange
            var adapter = new ProxyTelemetrySourceMethodAdapter();

            var listener = new Listener1();
            var method = GetMethodInfo<Listener1>(l => l.Listen());

            // Act
            var func = adapter.Adapt(method, new { }.GetType());

            // Assert
            Assert.False(func(listener, "hello"));
        }

        [Fact]
        public void Adapt_SplatsParameters()
        {
            // Arrange
            var adapter = new ProxyTelemetrySourceMethodAdapter();

            var listener = new Listener2();
            var value = new { id = 17, name = "Bill" };
            var method = GetMethodInfo<Listener2>(l => l.Listen(0, ""));

            // Act
            var func = adapter.Adapt(method, value.GetType());

            // Assert
            Assert.True(func(listener, value));
            Assert.Equal(17, listener.Id);
            Assert.Equal("Bill", listener.Name);
        }

        [Fact]
        public void Adapt_SplatsParameters_ExtraEventDataIgnored()
        {
            // Arrange
            var adapter = new ProxyTelemetrySourceMethodAdapter();

            var listener = new Listener2();
            var value = new { id = 17, name = "Bill", ignored = "hi" };
            var method = GetMethodInfo<Listener2>(l => l.Listen(0, ""));

            // Act
            var func = adapter.Adapt(method, value.GetType());

            // Assert
            Assert.True(func(listener, value));
            Assert.Equal(17, listener.Id);
            Assert.Equal("Bill", listener.Name);
        }

        [Fact]
        public void Adapt_SplatsParameters_ExtraParametersGetDefaultValues()
        {
            // Arrange
            var adapter = new ProxyTelemetrySourceMethodAdapter();

            var listener = new Listener2();
            var value = new { };
            var method = GetMethodInfo<Listener2>(l => l.Listen(0, ""));

            // Act
            var func = adapter.Adapt(method, value.GetType());

            // Assert
            Assert.True(func(listener, value));
            Assert.Equal(0, listener.Id);
            Assert.Null(listener.Name);
        }

        [Fact]
        public void Adapt_SplatsParameters_WithProxy()
        {
            // Arrange
            var adapter = new ProxyTelemetrySourceMethodAdapter();

            var listener = new Listener3();
            var value = new { id = 17, person = new Person() { Name = "Bill" } };
            var method = GetMethodInfo<Listener3>(l => l.Listen(0, null));

            // Act
            var func = adapter.Adapt(method, value.GetType());

            // Assert
            Assert.True(func(listener, value));
            Assert.Equal(17, listener.Id);
            Assert.Equal("Bill", listener.Name);
        }

        private MethodInfo GetMethodInfo<T>(Expression<Action<T>> expression)
        {
            var body = (MethodCallExpression)expression.Body;
            return body.Method;
        }

        private class Listener1
        {
            public void Listen()
            {
            }
        }

        private class Listener2
        {
            public int Id { get; set; } = 5;

            public string Name { get; set; } = "shouldn't be seen";

            public void Listen(int id, string name)
            {
                Id = id;
                Name = name;
            }
        }

        private class Listener3
        {
            public int Id { get; set; } = 5;

            public string Name { get; set; } = "shouldn't be seen";

            public void Listen(int id, IPerson person)
            {
                Id = id;
                Name = person?.Name;
            }
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
