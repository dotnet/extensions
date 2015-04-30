// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Framework.ConfigurationModel;
using Xunit;

namespace Microsoft.Framework.OptionsModel.Test
{
    public class ConfigurationBinderExceptionTests
    {
        [Fact]
        public void ExceptionWhenTryingToBindToInterface()
        {
            var input = new Dictionary<string, string>
            {
                {"ISomeInterfaceProperty:Subkey", "x"}
            };

            var config = new Configuration(new MemoryConfigurationSource(input));

            var exception = Assert.Throws<InvalidOperationException>(
                () => ConfigurationBinder.Bind<TestOptions>(config));
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

            var config = new Configuration(new MemoryConfigurationSource(input));

            var exception = Assert.Throws<InvalidOperationException>(
                () => ConfigurationBinder.Bind<TestOptions>(config));
            Assert.Equal(
                Resources.FormatError_MissingParameterlessConstructor(typeof(ClassWithoutPublicConstructor)),
                exception.Message);
        }

        [Fact]
        public void ExceptionWhenTryingToBindToTypeThatCannotBeConverted()
        {
            const string IncorrectValue = "This is not an int";

            var input = new Dictionary<string, string>
            {
                {"IntProperty", IncorrectValue}
            };

            var config = new Configuration(new MemoryConfigurationSource(input));

            var exception = Assert.Throws<InvalidOperationException>(
                () => ConfigurationBinder.Bind<TestOptions>(config));
            Assert.NotNull(exception.InnerException);
            Assert.Equal(
                Resources.FormatError_FailedBinding(IncorrectValue, typeof(int)),
                exception.Message);
        }

        [Fact]
        public void ExceptionWhenTryingToBindToTypeThrowsWhenActivated()
        {
            var input = new Dictionary<string, string>
            {
                {"ThrowsWhenActivatedProperty:subkey", "x"}
            };

            var config = new Configuration(new MemoryConfigurationSource(input));

            var exception = Assert.Throws<InvalidOperationException>(
                () => ConfigurationBinder.Bind<TestOptions>(config));
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

            var config = new Configuration(new MemoryConfigurationSource(input));

            var exception = Assert.Throws<InvalidOperationException>(
                () => ConfigurationBinder.Bind<TestOptions>(config));
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

        private class NestedOptions
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

            public NestedOptions NestedOptionsProperty { get; set; }
        }
    }
}
