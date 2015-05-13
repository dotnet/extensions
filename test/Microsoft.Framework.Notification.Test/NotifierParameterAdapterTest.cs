// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.Framework.Notification
{
    public class NotifierParameterAdapterTest
    {
        [Fact]
        public void Adapt_Null()
        {
            // Arrange
            var value = (object)null;

            var adapter = new NotifierParameterAdapter();

            // Act
            var result = adapter.Adapt(value, typeof(string));

            // Assert
            Assert.Null(result);
        }

        public static TheoryData<object, Type> Identity_ReferenceTypes_Data
        {
            get
            {
                return new TheoryData<object, Type>()
                {
                    { "Hello, world!", typeof(string) },
                    { new Person(), typeof(Person) },
                };
            }
        }

        [Theory]
        [MemberData(nameof(Identity_ReferenceTypes_Data))]
        public void Adapt_Identity_ReferenceTypes(object value, Type outputType)
        {
            // Arrange
            var adapter = new NotifierParameterAdapter();

            // Act
            var result = adapter.Adapt(value, outputType);

            // Assert
            Assert.IsType(outputType, result);
            Assert.Same(value, result);
        }

        public static TheoryData<object, Type> Identity_ValueTypes_Data
        {
            get
            {
                return new TheoryData<object, Type>()
                {
                    { 19, typeof(int) },
                    { new SomeValueType(17), typeof(SomeValueType) },
                };
            }
        }

        [Theory]
        [MemberData(nameof(Identity_ValueTypes_Data))]
        public void Adapt_Identity_ValueTypes(object value, Type outputType)
        {
            // Arrange
            var adapter = new NotifierParameterAdapter();

            // Act
            var result = adapter.Adapt(value, outputType);

            // Assert
            Assert.IsType(outputType, result);
            Assert.Same(value, result); // This works because of boxing
        }

        public static TheoryData<object, Type> Assignable_Data
        {
            get
            {
                return new TheoryData<object, Type>()
                {
                    { 5, typeof(IConvertible) }, // Interface assignment
                    { new DerivedPerson(), typeof(Person) }, // Base-class assignment
                    { 5.8m, typeof(decimal?) }, // value-type to nullable assignment
                };
            }
        }

        [Theory]
        [MemberData(nameof(Assignable_Data))]
        public void Adapt_Assignable(object value, Type outputType)
        {
            // Arrange
            var adapter = new NotifierParameterAdapter();

            // Act
            var result = adapter.Adapt(value, outputType);

            // Assert
            Assert.IsType(value.GetType(), result);
            Assert.IsAssignableFrom(outputType, result);
            Assert.Same(value, result);
        }

        [Fact]
        public void Adapt_Proxy_DestinationIsNotInterface()
        {
            // Arrange
            var value = new Person();
            var outputType = typeof(string);

            var expectedMessage = string.Format(
                "Type '{0}' must be an interface in order to support proxy generation from source type '{1}'.",
                outputType.FullName,
                value.GetType().FullName);

            var adapter = new NotifierParameterAdapter();

            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => adapter.Adapt(value, outputType));

            // Assert
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void Adapt_Proxy_InvalidProperty_DestinationIsNotInterface()
        {
            // Arrange
            var value = new Person();
            var outputType = typeof(IBadPerson);

            var expectedMessage = string.Format(
                "Type '{0}' must be an interface in order to support proxy generation from source type '{1}'.",
                typeof(string),
                typeof(Address).FullName);

            var adapter = new NotifierParameterAdapter();

            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => adapter.Adapt(value, outputType));

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

            var adapter = new NotifierParameterAdapter();

            // Act
            var result = adapter.Adapt(value, outputType);

            // Assert
            var person = Assert.IsAssignableFrom<IPerson>(result);
            Assert.Same(value.Address.City, person.Address.City);
            Assert.Same(value.Address.State, person.Address.State);
            Assert.Equal(value.Address.Zip, person.Address.Zip);

            // IPerson doesn't define the FirstName property.
            Assert.Same(value.LastName, person.LastName);
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

            var outputType = typeof(IC1);

            var adapter = new NotifierParameterAdapter();

            // Act
            var result = adapter.Adapt(value, outputType);

            // Assert
            var c1 = Assert.IsAssignableFrom<IC1>(result);
            Assert.Equal(value.C2.Tag, c1.C2.Tag);
            Assert.Equal(value.C2.C1.Tag, c1.C2.C1.Tag);
            Assert.Equal(value.C2.C1.C2.Tag, c1.C2.C1.C2.Tag);
            Assert.Null(value.C2.C1.C2.C1);
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
