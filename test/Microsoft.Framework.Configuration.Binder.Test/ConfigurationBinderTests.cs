// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Microsoft.Framework.Configuration.Memory;
using Xunit;

namespace Microsoft.Framework.Configuration.Binder.Test
{
    public class ConfigurationBinderTests
    {
        public class ComplexOptions
        {
            public ComplexOptions()
            {
                Nested = new NestedOptions();
                Virtual = "complex";
            }

            public NestedOptions Nested { get; set; }
            public int Integer { get; set; }
            public bool Boolean { get; set; }
            public virtual string Virtual { get; set; }

            public string PrivateSetter { get; private set; }
            public string ProtectedSetter { get; protected set; }
            public string InternalSetter { get; internal set; }
            public static string StaticProperty { get; set; }

            public string ReadOnly
            {
                get { return null; }
            }
        }

        public class NestedOptions
        {
            public int Integer { get; set; }
        }

        public class DerivedOptions : ComplexOptions
        {
            public override string Virtual
            {
                get
                {
                    return base.Virtual;
                }
                set
                {
                    base.Virtual = "Derived:" + value;
                }
            }
        }

        public class NullableOptions
        {
            public bool? MyNullableBool { get; set; }
            public int? MyNullableInt { get; set; }
            public DateTime? MyNullableDateTime { get; set; }
        }

        public class EnumOptions
        {
            public UriKind UriKind { get; set; }
        }

        public class GenericOptions<T>
        {
            public T Value { get; set; }
        }

        [Fact]
        public void GetScalarNullable()
        {
            var dic = new Dictionary<string, string>
            {
                {"Integer", "-2"},
                {"Boolean", "TRUe"},
                {"Nested:Integer", "11"}
            };
            var builder = new ConfigurationBuilder(new MemoryConfigurationProvider(dic));
            var config = builder.Build();

            Assert.True(config.Get<bool?>("Boolean"));
            Assert.Equal(-2, config.Get<int?>("Integer"));
            Assert.Equal(11, config.Get<int?>("Nested:Integer"));
        }

        [Fact]
        public void GetNullValue()
        {
            var dic = new Dictionary<string, string>
            {
                {"Integer", null},
                {"Boolean", null},
                {"Nested:Integer", null},
                {"Object", null }
            };
            var builder = new ConfigurationBuilder(new MemoryConfigurationProvider(dic));
            var config = builder.Build();

            Assert.False(config.Get<bool>("Boolean"));
            Assert.Equal(0, config.Get<int>("Integer"));
            Assert.Equal(0, config.Get<int>("Nested:Integer"));
            Assert.Null(config.Get<ComplexOptions>("Object"));
        }

        [Fact]
        public void GetDefaultsWhenDataDoesNotExist()
        {
            var dic = new Dictionary<string, string>
            {
            };
            var builder = new ConfigurationBuilder(new MemoryConfigurationProvider(dic));
            var config = builder.Build();

            Assert.False(config.Get<bool>("Boolean"));
            Assert.Equal(0, config.Get<int>("Integer"));
            Assert.Equal(0, config.Get<int>("Nested:Integer"));
            Assert.Null(config.Get<ComplexOptions>("Object"));
        }

#if !DNXCORE50 // TypeConverter doesn't support this on DNXCORE
        [Fact]
        public void GetUri()
        {
            var dic = new Dictionary<string, string>
            {
                {"AnUri", "http://www.bing.com"}
            };
            var builder = new ConfigurationBuilder(new MemoryConfigurationProvider(dic));
            var config = builder.Build();

            var uri = config.Get<Uri>("AnUri");

            Assert.Equal("http://www.bing.com", uri.OriginalString);
        }
#endif

        [Theory]
        [InlineData("2147483647", typeof(int))]
        [InlineData("4294967295", typeof(uint))]
        [InlineData("32767", typeof(short))]
        [InlineData("65535", typeof(ushort))]
        [InlineData("-9223372036854775808", typeof(long))]
        [InlineData("18446744073709551615", typeof(ulong))]
        [InlineData("trUE", typeof(bool))]
        [InlineData("255", typeof(byte))]
        [InlineData("127", typeof(sbyte))]
        [InlineData("\uffff", typeof(char))]
        [InlineData("79228162514264337593543950335", typeof(decimal))]
        [InlineData("1.79769e+308", typeof(double))]
        [InlineData("3.40282347E+38", typeof(float))]
        [InlineData("2015-12-24T07:34:42-5:00", typeof(DateTime))]
        [InlineData("12/24/2015 13:44:55 +4", typeof(DateTimeOffset))]
        [InlineData("99.22:22:22.1234567", typeof(TimeSpan))]
#if !DNXCORE50 // TypeConverter doesn't support this on DNXCORE
        [InlineData("http://www.bing.com", typeof(Uri))]
#endif
        // enum test
        [InlineData("Constructor", typeof(AttributeTargets))]
        [InlineData("CA761232-ED42-11CE-BACD-00AA0057B223", typeof(Guid))]
        public void CanReadAllSupportedTypes(string value, Type type)
        {
            // arrange
            var dic = new Dictionary<string, string>
            {
                {"Value", value}
            };
            var builder = new ConfigurationBuilder(new MemoryConfigurationProvider(dic));
            var config = builder.Build();

            var optionsType = typeof(GenericOptions<>).MakeGenericType(type);
            var options = Activator.CreateInstance(optionsType);
            var expectedValue = TypeDescriptor.GetConverter(type).ConvertFromInvariantString(value);

            // act
            config.Bind(options);
            var optionsValue = options.GetType().GetProperty("Value").GetValue(options);
            var getValue = config.Get(type, "Value");

            // assert
            Assert.Equal(expectedValue, optionsValue);
            Assert.Equal(expectedValue, getValue);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(uint))]
        [InlineData(typeof(short))]
        [InlineData(typeof(ushort))]
        [InlineData(typeof(long))]
        [InlineData(typeof(ulong))]
        [InlineData(typeof(bool))]
        [InlineData(typeof(byte))]
        [InlineData(typeof(sbyte))]
        [InlineData(typeof(char))]
        [InlineData(typeof(decimal))]
        [InlineData(typeof(double))]
        [InlineData(typeof(float))]
        [InlineData(typeof(DateTime))]
        [InlineData(typeof(DateTimeOffset))]
        [InlineData(typeof(TimeSpan))]
        [InlineData(typeof(AttributeTargets))]
        [InlineData(typeof(Guid))]
        public void ConsistentExceptionOnFailedBinding(Type type)
        {
            // arrange
            const string IncorrectValue = "Invalid data";
            var dic = new Dictionary<string, string>
            {
                {"Value", IncorrectValue}
            };
            var builder = new ConfigurationBuilder(new MemoryConfigurationProvider(dic));
            var config = builder.Build();

            var optionsType = typeof(GenericOptions<>).MakeGenericType(type);
            var options = Activator.CreateInstance(optionsType);

            // act
            var exception = Assert.Throws<InvalidOperationException>(
                () => config.Bind(options));

            var getException = Assert.Throws<InvalidOperationException>(
                () => config.Get(type, "Value"));

            // assert
            Assert.NotNull(exception.InnerException);
            Assert.NotNull(getException.InnerException);
            Assert.Equal(
                Resources.FormatError_FailedBinding(IncorrectValue, type),
                exception.Message);
            Assert.Equal(
                Resources.FormatError_FailedBinding(IncorrectValue, type),
                getException.Message);
        }

        [Fact]
        public void BinderIgnoresIndexerProperties()
        {
            var builder = new ConfigurationBuilder();
            var config = builder.Build();
            config.Bind(new List<string>());
        }

        [Fact]
        public void BindCanReadComplexProperties()
        {
            var dic = new Dictionary<string, string>
            {
                {"Integer", "-2"},
                {"Boolean", "TRUe"},
                {"Nested:Integer", "11"}
            };
            var builder = new ConfigurationBuilder(new MemoryConfigurationProvider(dic));
            var config = builder.Build();
            
            var instance = new ComplexOptions();
            config.Bind(instance);
            
            Assert.True(instance.Boolean);
            Assert.Equal(-2, instance.Integer);
            Assert.Equal(11, instance.Nested.Integer);
        }

        [Fact]
        public void GetCanReadComplexProperties()
        {
            var dic = new Dictionary<string, string>
            {
                {"Integer", "-2"},
                {"Boolean", "TRUe"},
                {"Nested:Integer", "11"}
            };
            var builder = new ConfigurationBuilder(new MemoryConfigurationProvider(dic));
            var config = builder.Build();

            var options = config.Get<ComplexOptions>();

            Assert.True(options.Boolean);
            Assert.Equal(-2, options.Integer);
            Assert.Equal(11, options.Nested.Integer);
        }

        [Fact]
        public void BindCanReadInheritedProperties()
        {
            var dic = new Dictionary<string, string>
            {
                {"Integer", "-2"},
                {"Boolean", "TRUe"},
                {"Nested:Integer", "11"},
                {"Virtual", "Sup"}
            };
            var builder = new ConfigurationBuilder(new MemoryConfigurationProvider(dic));
            var config = builder.Build();
            
            var instance = new DerivedOptions();
            config.Bind(instance);
            
            Assert.True(instance.Boolean);
            Assert.Equal(-2, instance.Integer);
            Assert.Equal(11, instance.Nested.Integer);
            Assert.Equal("Derived:Sup", instance.Virtual);
        }

        [Fact]
        public void GetCanReadInheritedProperties()
        {
            var dic = new Dictionary<string, string>
            {
                {"Integer", "-2"},
                {"Boolean", "TRUe"},
                {"Nested:Integer", "11"},
                {"Virtual", "Sup"}
            };
            var builder = new ConfigurationBuilder(new MemoryConfigurationProvider(dic));
            var config = builder.Build();

            var options = config.Get<DerivedOptions>();

            Assert.True(options.Boolean);
            Assert.Equal(-2, options.Integer);
            Assert.Equal(11, options.Nested.Integer);
            Assert.Equal("Derived:Sup", options.Virtual);
        }

        [Fact]
        public void GetCanReadStaticProperty()
        {
            var dic = new Dictionary<string, string>
            {
                {"StaticProperty", "stuff"},
            };
            var builder = new ConfigurationBuilder(new MemoryConfigurationProvider(dic));
            var config = builder.Build();

            var options = config.Get<ComplexOptions>();

            Assert.Equal("stuff", ComplexOptions.StaticProperty);
        }

        [Fact]
        public void BindCanReadStaticProperty()
        {
            var dic = new Dictionary<string, string>
            {
                {"StaticProperty", "other stuff"},
            };
            var builder = new ConfigurationBuilder(new MemoryConfigurationProvider(dic));
            var config = builder.Build();

            var instance = new ComplexOptions();
            config.Bind(instance);

            Assert.Equal("other stuff", ComplexOptions.StaticProperty);
        }

        [Theory]
        [InlineData("ReadOnly")]
        [InlineData("PrivateSetter")]
        [InlineData("ProtectedSetter")]
        [InlineData("InternalSetter")]
        public void GetIgnoresTests(string property)
        {
            var dic = new Dictionary<string, string>
            {
                {property, "stuff"},
            };
            var builder = new ConfigurationBuilder(new MemoryConfigurationProvider(dic));
            var config = builder.Build();

            var options = config.Get<ComplexOptions>();
            Assert.Null(options.GetType().GetTypeInfo().GetDeclaredProperty(property).GetValue(options));
        }

        [Theory]
        [InlineData("ReadOnly")]
        [InlineData("PrivateSetter")]
        [InlineData("ProtectedSetter")]
        [InlineData("InternalSetter")]
        public void BindIgnoresTests(string property)
        {
            var dic = new Dictionary<string, string>
            {
                {property, "stuff"},
            };
            var builder = new ConfigurationBuilder(new MemoryConfigurationProvider(dic));
            var config = builder.Build();

            var options = new ComplexOptions();
            config.Bind(options);

            Assert.Null(options.GetType().GetTypeInfo().GetDeclaredProperty(property).GetValue(options));
        }

        [Fact]
        public void ExceptionWhenTryingToBindToInterface()
        {
            var input = new Dictionary<string, string>
            {
                {"ISomeInterfaceProperty:Subkey", "x"}
            };

            var builder = new ConfigurationBuilder(new MemoryConfigurationProvider(input));
            var config = builder.Build();

            var exception = Assert.Throws<InvalidOperationException>(
                () => config.Get<TestOptions>());
            Assert.Equal(
                Resources.FormatError_CannotActivateAbstractOrInterface(typeof(ISomeInterface)),
                exception.Message);
        }

        [Fact]
        public void ExceptionWhenTryingToBindClassWithoutParameterlessConstructor()
        {
            var input = new Dictionary<string, string>
            {
                {"ClassWithoutPublicConstructorProperty:Subkey", "x"}
            };

            var builder = new ConfigurationBuilder(new MemoryConfigurationProvider(input));
            var config = builder.Build();

            var exception = Assert.Throws<InvalidOperationException>(
                () => config.Get<TestOptions>());
            Assert.Equal(
                Resources.FormatError_MissingParameterlessConstructor(typeof(ClassWithoutPublicConstructor)),
                exception.Message);
        }

        [Fact]
        public void ExceptionWhenTryingToBindToTypeThrowsWhenActivated()
        {
            var input = new Dictionary<string, string>
            {
                {"ThrowsWhenActivatedProperty:subkey", "x"}
            };

            var builder = new ConfigurationBuilder(new MemoryConfigurationProvider(input));
            var config = builder.Build();

            var exception = Assert.Throws<InvalidOperationException>(
                () => config.Get<TestOptions>());
            Assert.NotNull(exception.InnerException);
            Assert.Equal(
                Resources.FormatError_FailedToActivate(typeof(ThrowsWhenActivated)),
                exception.Message);
        }

        [Fact]
        public void ExceptionIncludesKeyOfFailedBinding()
        {
            var input = new Dictionary<string, string>
            {
                {"NestedOptionsProperty:NestedOptions2Property:ISomeInterfaceProperty:subkey", "x"}
            };

            var builder = new ConfigurationBuilder(new MemoryConfigurationProvider(input));
            var config = builder.Build();

            var exception = Assert.Throws<InvalidOperationException>(
                () => config.Get<TestOptions>());
            Assert.Equal(
                Resources.FormatError_CannotActivateAbstractOrInterface(typeof(ISomeInterface)),
                exception.Message);
        }

        private interface ISomeInterface
        {
        }

        private class ClassWithoutPublicConstructor
        {
            private ClassWithoutPublicConstructor()
            {
            }
        }

        private class ThrowsWhenActivated
        {
            public ThrowsWhenActivated()
            {
                throw new Exception();
            }
        }

        private class NestedOptions1
        {
            public NestedOptions2 NestedOptions2Property { get; set; }
        }

        private class NestedOptions2
        {
            public ISomeInterface ISomeInterfaceProperty { get; set; }
        }

        private class TestOptions
        {
            public ISomeInterface ISomeInterfaceProperty { get; set; }

            public ClassWithoutPublicConstructor ClassWithoutPublicConstructorProperty { get; set; }

            public int IntProperty { get; set; }

            public ThrowsWhenActivated ThrowsWhenActivatedProperty { get; set; }

            public NestedOptions1 NestedOptionsProperty { get; set; }
        }
    }
}