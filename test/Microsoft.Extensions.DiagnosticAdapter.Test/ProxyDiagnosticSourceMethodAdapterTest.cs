// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using System.Reflection;
using Xunit;

namespace Microsoft.Extensions.DiagnosticAdapter
{
    public class ProxyDiagnosticSourceMethodAdapterTest
    {
        [Fact]
        public void Adapt_Throws_ForSameNamedPropertiesWithDifferentCasing()
        {
            // Arrange
            var adapter = new ProxyDiagnosticSourceMethodAdapter();

            var listener = new Listener1();
            var method = GetMethodInfo<Listener2>(l => l.Listen(5, "joey"));

            var value = new { Id = 5, id = 17 };
            var type = value.GetType();

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => adapter.Adapt(method, type));

            // Assert
            Assert.Equal(
                $"Proxy method generation doesn't support types with properties that vary only by case. " +
                $"The type '{type.FullName}' defines multiple properties named 'id' that vary only by case.",
                ex.Message);
        }

        [Fact]
        public void Adapt_ReturnsTrueForTypeMatch()
        {
            // Arrange
            var adapter = new ProxyDiagnosticSourceMethodAdapter();

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
            var adapter = new ProxyDiagnosticSourceMethodAdapter();

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
            var adapter = new ProxyDiagnosticSourceMethodAdapter();

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
        public void Adapt_SplatsParameters_CamelCase()
        {
            // Arrange
            var adapter = new ProxyDiagnosticSourceMethodAdapter();

            var listener = new Listener4();
            var value = new { Id = 17, Person = new Person() { Name = "Bill" } };
            var method = GetMethodInfo<Listener4>(l => l.Listen(0, null));

            // Act
            var func = adapter.Adapt(method, value.GetType());

            // Assert
            Assert.True(func(listener, value));
            Assert.Equal(17, listener.Id);
            Assert.Equal("Bill", listener.Name);
        }

        [Fact]
        public void Adapt_SplatsParameters_CaseInsensitive()
        {
            // Arrange
            var adapter = new ProxyDiagnosticSourceMethodAdapter();

            var listener = new Listener4();
            var value = new { ID = 17, PersOn = new Person() { Name = "Bill" }};
            var method = GetMethodInfo<Listener4>(l => l.Listen(0, null));

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
            var adapter = new ProxyDiagnosticSourceMethodAdapter();

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
            var adapter = new ProxyDiagnosticSourceMethodAdapter();

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
            var adapter = new ProxyDiagnosticSourceMethodAdapter();

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

        [Fact]
        public void Adapt_SplatsParameters_WithNominalType()
        {
            // Arrange
            var adapter = new ProxyDiagnosticSourceMethodAdapter();

            var listener = new Listener3();
            var value = new NominalType() { Id = 17, Person = new Person() { Name = "Bill" } };
            var method = GetMethodInfo<Listener3>(l => l.Listen(0, null));

            // Act
            var func = adapter.Adapt(method, value.GetType());

            // Assert
            Assert.True(func(listener, value));
            Assert.Equal(17, listener.Id);
            Assert.Equal("Bill", listener.Name);
        }

        [Fact]
        public void CanCreateProxyMethodForBasicType()
        {
            // Arrange
            var target = new Listener5();
            var source = new { name = "John", age = 1234 };

            var targetMethodInfo = target.GetType().GetMethod(nameof(Listener5.TargetMethod));

            // Act
            var adapter = new ProxyDiagnosticSourceMethodAdapter();
            var callback = adapter.Adapt(targetMethodInfo, source.GetType());

            var result = callback(target, source);

            // Assert
            Assert.True(result);
            Assert.Equal(target.Name, source.name);
            Assert.Equal(target.Age, source.age);
        }

        [Fact]
        public void CanCreateProxyMethodForBasicTypeWithUpperCasing()
        {
            // Arrange
            var target = new Listener6();
            var source = new { Name = "John", Age = 1234 };

            var targetMethodInfo = target.GetType().GetMethod(nameof(Listener6.Listen));

            // Act
            var adapter = new ProxyDiagnosticSourceMethodAdapter();
            var callback = adapter.Adapt(targetMethodInfo, source.GetType());

            var result = callback(target, source);

            // Assert
            Assert.True(result);
            Assert.Equal(target.SafeName, source.Name);
            Assert.Equal(target.SafeAge, source.Age);
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

        private class Listener4
        {
            public int Id { get; set; } = 5;

            public string Name { get; set; }

            public void Listen(int Id, IPerson Person)
            {
                this.Id = Id;
                this.Name = Person?.Name;
            }
        }

        public class Listener5
        {
            public void TargetMethod(string name, int age)
            {
                Name = name;
                Age = age;
            }

            public string Name { get; set; }

            public int Age { get; set; }
        }

        public class Listener6
        {
            public void Listen(string Name, int Age)
            {
                SafeName = Name;
                SafeAge = Age;
            }

            public string SafeName { get; set; }

            public int SafeAge { get; set; }
        }

        public class NominalType
        {
            public int Id { get; set; }

            public Person Person { get; set; }
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
