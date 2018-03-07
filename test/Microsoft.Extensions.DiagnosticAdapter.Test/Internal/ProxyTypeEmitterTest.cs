// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.DiagnosticAdapter.Internal
{
    public class ProxyTypeEmitterTest
    {
        public static TheoryData<Type> GetProxyType_Identity_Data
        {
            get
            {
                return new TheoryData<Type>()
                {
                    { typeof(string) },
                    { typeof(Person) },
                    { typeof(int) },
                    { typeof(SomeValueType) },
                };
            }
        }

        [Theory]
        [MemberData(nameof(GetProxyType_Identity_Data))]
        public void GetProxyType_Identity(Type type)
        {
            // Arrange
            var cache = new ProxyTypeCache();

            // Act
            var result = ProxyTypeEmitter.GetProxyType(cache, type, type);

            // Assert
            Assert.Null(result);
        }

        public static TheoryData<Type, Type> GetProxyType_Assignable_Data
        {
            get
            {
                return new TheoryData<Type, Type>()
                {
                    { typeof(int), typeof(IConvertible) }, // Interface assignment
                    { typeof(DerivedPerson), typeof(Person) }, // Base-class assignment
                    { typeof(decimal), typeof(decimal?) }, // value-type to nullable assignment
                };
            }
        }

        [Theory]
        [MemberData(nameof(GetProxyType_Assignable_Data))]
        public void GetProxyType_Assignable(Type sourceType, Type targetType)
        {
            // Arrange
            var cache = new ProxyTypeCache();

            // Act
            var result = ProxyTypeEmitter.GetProxyType(cache, targetType, sourceType);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetProxyType_IfAlreadyInCache_AlsoAddedToVisited_FromType()
        {
            // Arrange
            var targetType = typeof(IPerson);
            var sourceType = typeof(Person);

            var key = new Tuple<Type, Type>(sourceType, targetType);
            var cache = new ProxyTypeCache();
            cache[key] = ProxyTypeCacheResult.FromType(key, sourceType, sourceType.GetConstructor(Array.Empty<Type>()));

            var context = new ProxyTypeEmitter.ProxyBuilderContext(cache, targetType, sourceType);

            // Act
            var result = ProxyTypeEmitter.VerifyProxySupport(context, key);
            var result2 = ProxyTypeEmitter.VerifyProxySupport(context, key);

            // Assert
            Assert.True(result);
            Assert.True(result2);
            Assert.Single(context.Visited);
            Assert.Equal(key, context.Visited.Single().Key);
        }

        [Fact]
        public void GetProxyType_IfAlreadyInCache_AlsoAddedToVisited_FromError()
        {
            // Arrange
            var targetType = typeof(IPerson);
            var sourceType = typeof(Person);

            var key = new Tuple<Type, Type>(sourceType, targetType);
            var cache = new ProxyTypeCache();
            cache[key] = ProxyTypeCacheResult.FromError(key, "Test Error");

            var context = new ProxyTypeEmitter.ProxyBuilderContext(cache, targetType, sourceType);

            // Act
            var result = ProxyTypeEmitter.VerifyProxySupport(context, key);

            // Assert
            Assert.False(result);
            Assert.Equal(key, context.Visited.Single().Key);
        }

        [Fact]
        public void GetProxyType_Fails_DestinationIsNotInterface()
        {
            // Arrange
            var sourceType = typeof(Person);
            var targetType = typeof(string);

            var expectedMessage = string.Format(
                "Type '{0}' must be an interface in order to support proxy generation from source type '{1}'.",
                targetType.FullName,
                sourceType.FullName);

            // Act
            var exception = Assert.Throws<InvalidProxyOperationException>(
                () => ProxyTypeEmitter.GetProxyType(new ProxyTypeCache(), targetType, sourceType));

            // Assert
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void Adapt_Proxy_InvalidProperty_DestinationIsNotInterface()
        {
            // Arrange
            var sourceType = typeof(Person);
            var targetType = typeof(IBadPerson);

            var expectedMessage = string.Format(
                "Type '{0}' must be an interface in order to support proxy generation from source type '{1}'.",
                typeof(string),
                typeof(Address).FullName);

            // Act
            var exception = Assert.Throws<InvalidProxyOperationException>(
                () => ProxyTypeEmitter.GetProxyType(new ProxyTypeCache(), targetType, sourceType));

            // Assert
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void Adapt_Proxy()
        {
            // Arrange
            var value = new Person()
            {
                Address = new Address()
                {
                    City = "Redmond",
                    State = "WA",
                    Zip = 98002,
                },
                FirstName = "Bill",
                LastName = "Gates",
            };

            var outputType = typeof(IPerson);

            // Act
            var result = ConvertTo<IPerson>(value);

            // Assert
            var person = Assert.IsAssignableFrom<IPerson>(result);
            Assert.Same(value.Address.City, person.Address.City);
            Assert.Same(value.Address.State, person.Address.State);
            Assert.Equal(value.Address.Zip, person.Address.Zip);

            // IPerson doesn't define the FirstName property.
            Assert.Same(value.LastName, person.LastName);
        }

        [Fact]
        public void Adapt_Proxy_NullProperty()
        {
            // Arrange
            var value = new Person()
            {
                Address = null,
                FirstName = null,
                LastName = null,
            };

            var outputType = typeof(IPerson);

            // Act
            var result = ConvertTo<IPerson>(value);

            // Assert
            var person = Assert.IsAssignableFrom<IPerson>(result);
            Assert.Null(person.Address);
            Assert.Null(person.FirstName);
            Assert.Null(person.LastName);
        }

        [Fact]
        public void Adapt_Proxy_WithTypeCycle()
        {
            // Arrange
            var value = new C1()
            {
                C2 = new C2()
                {
                    C1 = new C1()
                    {
                        C2 = new C2(),
                        Tag = "C1.C2.C1",
                    },
                    Tag = "C1.C2",
                },
                Tag = "C1",
            };

            // Act
            var result = ConvertTo<IC1>(value);

            // Assert
            var c1 = Assert.IsAssignableFrom<IC1>(result);
            Assert.Equal(value.C2.Tag, c1.C2.Tag);
            Assert.Equal(value.C2.C1.Tag, c1.C2.C1.Tag);
            Assert.Equal(value.C2.C1.C2.Tag, c1.C2.C1.C2.Tag);
            Assert.Null(value.C2.C1.C2.C1);
        }

        [Fact]
        public void Adapt_Proxy_WithPrivatePropertyGetterOnSourceType_ReferenceTypeProperty()
        {
            // Arrange
            var value = new PrivateGetter()
            {
                Ignored = "hi",
            };

            // Act
            var result = ConvertTo<IPrivateGetter>(value);

            // Assert
            var proxy = Assert.IsAssignableFrom<IPrivateGetter>(result);
            Assert.Null(proxy.Ignored);
        }

        [Fact]
        public void Adapt_Proxy_WithPrivatePropertyGetterOnSourceType_ValueTypeProperty()
        {
            // Arrange
            var value = new PrivateGetter();

            // Act
            var result = ConvertTo<IPrivateGetter>(value);

            // Assert
            var proxy = Assert.IsAssignableFrom<IPrivateGetter>(result);
            Assert.Equal(0, proxy.IgnoredAlso);
        }

        [Fact]
        public void Adapt_InvalidProxy_IndexerProperty()
        {
            // Arrange
            var sourceType = typeof(Indexer);
            var targetType = typeof(IIndexer);

            var expected =
                $"The property 'Item' on type '{targetType}' must not define a setter to support proxy generation.";

            // Act & Assert
            var exception = Assert.Throws<InvalidProxyOperationException>(
                () => ProxyTypeEmitter.GetProxyType(new ProxyTypeCache(), targetType, sourceType));
            Assert.Equal(expected, exception.Message);
        }

        [Fact]
        public void Adapt_List_ToReadOnlyList()
        {
            // Arrange
            var value = new List<string>()
            {
                "Hello",
                "World",
            };

            // Act 
            var proxy = Convert<IList<string>, IReadOnlyList<string>>(value);

            // Assert
            Assert.NotNull(proxy);
            Assert.Equal(2, proxy.Count);
            Assert.Equal("Hello", proxy[0]);
            Assert.Equal("World", proxy[1]);
        }

        [Fact]
        public void Adapt_List_ToReadOnlyList_Enumerator()
        {
            // Arrange
            var value = new List<string>()
            {
                "Hello",
                "World",
            };

            // Act 
            var proxy = Convert<IList<string>, IReadOnlyList<string>>(value);

            // Assert
            Assert.NotNull(proxy);

            var sequence = value.Zip(proxy, (i, j) => Tuple.Create(i, j));
            foreach (var item in sequence)
            {
                Assert.Equal(item.Item1, item.Item2);
            }
        }

        [Fact]
        public void Adapt_ListWithProxy_ToReadOnlyList()
        {
            // Arrange
            var value = new List<Person>()
            {
                new Person() { FirstName = "Billy" },
                new Person() { FirstName = "Joe" },
            };

            // Act 
            var proxy = Convert<IList<Person>, IReadOnlyList<IPerson>>(value);

            // Assert
            Assert.NotNull(proxy);
            Assert.Equal(2, proxy.Count);
            Assert.Equal("Billy", proxy[0].FirstName);
            Assert.Equal("Joe", proxy[1].FirstName);
        }

        [Fact]
        public void Adapt_ListWithProxy_ToReadOnlyList_Null()
        {
            // Arrange
            var value = new List<Person>()
            {
                new Person() { FirstName = "Billy" },
                null,
            };

            // Act 
            var proxy = Convert<IList<Person>, IReadOnlyList<IPerson>>(value);

            // Assert
            Assert.NotNull(proxy);
            Assert.Equal(2, proxy.Count);
            Assert.Equal("Billy", proxy[0].FirstName);
            Assert.Null(proxy[1]);
        }

        [Fact]
        public void Adapt_ListWithProxy_ToReadOnlyList_Enumerator()
        {
            // Arrange
            var value = new List<Person>()
            {
                new Person() { FirstName = "Billy" },
                new Person() { FirstName = "Joe" },
            };

            // Act 
            var proxy = Convert<IList<Person>, IReadOnlyList<IPerson>>(value);

            // Assert
            Assert.NotNull(proxy);

            var sequence = value.Zip(proxy, (i, j) => Tuple.Create(i, j));
            foreach (var item in sequence)
            {
                Assert.Equal(item.Item1.FirstName, item.Item2.FirstName);
            }
        }

        [Fact]
        public void Adapt_ListWithProxy_ToReadOnlyList_Enumerator_Null()
        {
            // Arrange
            var value = new List<Person>()
            {
                new Person() { FirstName = "Billy" },
                null,
            };

            // Act 
            var proxy = Convert<IList<Person>, IReadOnlyList<IPerson>>(value);

            // Assert
            Assert.NotNull(proxy);

            var sequence = value.Zip(proxy, (i, j) => Tuple.Create(i, j));
            foreach (var item in sequence)
            {
                Assert.Equal(item.Item1?.FirstName, item.Item2?.FirstName);
            }
        }

        [Fact]
        public void Adapt_Array_ToReadOnlyList()
        {
            // Arrange
            var value = new string[]
            {
                "Hello",
                "World",
            };

            // Act 
            var proxy = Convert<IList<string>, IReadOnlyList<string>>(value);

            // Assert
            Assert.NotNull(proxy);
            Assert.Equal(2, proxy.Count);
            Assert.Equal("Hello", proxy[0]);
            Assert.Equal("World", proxy[1]);
        }

        [Fact]
        public void Adapt_Array_ToReadOnlyList_Enumerator()
        {
            // Arrange
            var value = new string[]
            {
                "Hello",
                "World",
            };

            // Act 
            var proxy = Convert<IList<string>, IReadOnlyList<string>>(value);

            // Assert
            Assert.NotNull(proxy);

            var sequence = value.Zip(proxy, (i, j) => Tuple.Create(i, j));
            foreach (var item in sequence)
            {
                Assert.Equal(item.Item1, item.Item2);
            }
        }

        [Fact]
        public void Adapt_ListProperty_ToReadOnlyList()
        {
            // Arrange
            var value = new HasListProperty()
            {
                ListProperty = new List<string>()
                {
                    "Hello",
                    "World",
                },
            };

            // Act 
            var proxy = ConvertTo<IHasReadOnlyListProperty>(value);

            // Assert
            Assert.NotNull(proxy.ListProperty);
            Assert.Equal(2, proxy.ListProperty.Count);
            Assert.Equal("Hello", proxy.ListProperty[0]);
            Assert.Equal("World", proxy.ListProperty[1]);
        }

        [Fact]
        public void Adapt_ListListProperty_ToReadOnlyList_Enumerator()
        {
            // Arrange
            var value = new HasListProperty()
            {
                ListProperty = new List<string>()
                {
                    "Hello",
                    "World",
                },
            };

            // Act 
            var proxy = ConvertTo<IHasReadOnlyListProperty>(value);

            // Assert
            Assert.NotNull(proxy.ListProperty);

            var sequence = value.ListProperty.Zip(proxy.ListProperty, (i, j) => Tuple.Create(i, j));
            foreach (var item in sequence)
            {
                Assert.Equal(item.Item1, item.Item2);
            }
        }

        [Fact]
        public void Adapt_ArrayListProperty_ToReadOnlyList()
        {
            // Arrange
            var value = new HasArrayProperty()
            {
                ListProperty = new string[]
                {
                    "Hello",
                    "World",
                },
            };

            // Act 
            var proxy = ConvertTo<IHasReadOnlyListProperty>(value);

            // Assert
            Assert.NotNull(proxy.ListProperty);
            Assert.Equal(2, proxy.ListProperty.Count);
            Assert.Equal("Hello", proxy.ListProperty[0]);
            Assert.Equal("World", proxy.ListProperty[1]);
        }

        [Fact]
        public void Adapt_ArrayListProperty_ToReadOnlyList_Enumerator()
        {
            // Arrange
            var value = new HasArrayProperty()
            {
                ListProperty = new string[]
                {
                    "Hello",
                    "World",
                },
            };

            // Act 
            var proxy = ConvertTo<IHasReadOnlyListProperty>(value);

            // Assert
            Assert.NotNull(proxy.ListProperty);

            var sequence = value.ListProperty.Zip(proxy.ListProperty, (i, j) => Tuple.Create(i, j));
            foreach (var item in sequence)
            {
                Assert.Equal(item.Item1, item.Item2);
            }
        }

        [Fact]
        public void Adapt_ListPropertyWithProxy_ToReadOnlyList()
        {
            // Arrange
            var value = new HasListOfPersonProperty()
            {
                ListProperty = new List<Person>()
                {
                    new Person() { FirstName = "Billy" },
                    new Person() { FirstName = "Joe" },
                }
            };

            // Act 
            var proxy = ConvertTo<IHasReadOnlyListOfPersonProperty>(value);

            // Assert
            Assert.NotNull(proxy);
            Assert.Equal(2, proxy.ListProperty.Count);
            Assert.Equal("Billy", proxy.ListProperty[0].FirstName);
            Assert.Equal("Joe", proxy.ListProperty[1].FirstName);
        }

        [Fact]
        public void Adapt_ListPropertyWithProxy_ToReadOnlyList_Enumerator()
        {
            // Arrange
            var value = new HasListOfPersonProperty()
            {
                ListProperty = new List<Person>()
                {
                    new Person() { FirstName = "Billy" },
                    new Person() { FirstName = "Joe" },
                }
            };

            // Act 
            var proxy = ConvertTo<IHasReadOnlyListOfPersonProperty>(value);

            // Assert
            Assert.NotNull(proxy);

            var sequence = value.ListProperty.Zip(proxy.ListProperty, (i, j) => Tuple.Create(i, j));
            foreach (var item in sequence)
            {
                Assert.Equal(item.Item1.FirstName, item.Item2.FirstName);
            }
        }

        [Fact]
        public void Adapt_NestedList()
        {
            // Arrange
            var value = new List<IList<IList<Person>>>()
            {
                new List<IList<Person>>()
                {
                    new List<Person>()
                    {
                        new Person() { FirstName = "Billy" },
                    },
                },
            };

            // Act 
            var proxy = Convert<IList<IList<IList<Person>>>, IReadOnlyList<IReadOnlyList<IReadOnlyList<IPerson>>>>(value);

            // Assert
            Assert.NotNull(proxy);
            Assert.Equal(1, proxy[0][0].Count);
            Assert.Equal("Billy", proxy[0][0][0].FirstName);
        }

        [Fact]
        public void GetProxyType_LocksPreventDuplicateAssemblyNamesArgumentException_ForConcurrentThreads()
        {
            for (var i = 0; i < 5; i++)
            {
                Parallel.For(
                0,
                100,
                (j) =>
                {
                    var testObject = new Person();
                    ProxyTypeEmitter.GetProxyType(new ProxyTypeCache(), typeof(IPerson), testObject.GetType());
                });
            }
        }

        private object ConvertTo(object value, Type type)
        {
            var cache = new ProxyTypeCache();
            var proxyType = ProxyTypeEmitter.GetProxyType(cache, type, value.GetType());

            Assert.NotNull(proxyType);
            var proxy = Activator.CreateInstance(proxyType, value);

            Assert.IsAssignableFrom(type, proxy);
            return proxy;
        }

        private T ConvertTo<T>(object value)
        {
            var cache = new ProxyTypeCache();
            var proxyType = ProxyTypeEmitter.GetProxyType(cache, typeof(T), value.GetType());

            Assert.NotNull(proxyType);
            var proxy = Activator.CreateInstance(proxyType, value);

            return Assert.IsAssignableFrom<T>(proxy);
        }

        private U Convert<T, U>(object value)
        {
            var cache = new ProxyTypeCache();
            var proxyType = ProxyTypeEmitter.GetProxyType(cache, typeof(U), typeof(T));

            Assert.NotNull(proxyType);
            var proxy = Activator.CreateInstance(proxyType, value);

            return Assert.IsAssignableFrom<U>(proxy);
        }

        public interface IHasReadOnlyListProperty
        {
            IReadOnlyList<string> ListProperty { get; }
        }

        public class HasListProperty
        {
            public IList<string> ListProperty { get; set; }
        }

        public class HasArrayProperty
        {
            public string[] ListProperty { get; set; }
        }

        public class HasReadOnlyListProperty
        {
            public IReadOnlyList<string> ListProperty { get; set; }
        }

        public interface IHasReadOnlyListOfPersonProperty
        {
            IReadOnlyList<IPerson> ListProperty { get; }
        }

        public class HasListOfPersonProperty
        {
            public IList<Person> ListProperty { get; set; }
        }

        public interface IPrivateGetter
        {
            string Ignored { get; }

            int IgnoredAlso { get; }
        }

        public class PrivateGetter
        {
            public string Ignored { private get; set; }

            private int IgnoredAlso { get; set; } = 17;
        }

        public interface IIndexer
        {
            int this[int key] { get; set; }
        }

        public class Indexer
        {
            public int this[int key]
            {
                get { return 0; }
                set { }
            }
        }

        public interface IC1
        {
            IC2 C2 { get; }
            string Tag { get; }
        }

        public interface IC2
        {
            IC1 C1 { get; }
            string Tag { get; }
        }

        public class C1
        {
            public C2 C2 { get; set; }
            public string Tag { get; set; }
        }

        public class C2
        {
            public C1 C1 { get; set; }
            public string Tag { get; set; }
        }

        public interface IPerson
        {
            string FirstName { get; }
            string LastName { get; }
            IAddress Address { get; }
        }

        public interface IAddress
        {
            string City { get; }
            string State { get; }
            int Zip { get; }
        }

        public class Person
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public Address Address { get; set; }
        }

        public interface IBadPerson
        {
            string FirstName { get; }
            string LastName { get; }
            string Address { get; } // doesn't match with Person
        }

        public class DerivedPerson : Person
        {
            public double CoolnessFactor { get; set; }
        }

        public class Address
        {
            public string City { get; set; }
            public string State { get; set; }
            public int Zip { get; set; }
        }

        public class SomeValueType
        {
            public SomeValueType(int value)
            {
                Value = value;
            }

            public int Value { get; private set; }
        }
    }
}
