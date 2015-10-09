// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Dnx.Runtime;
using Xunit;

namespace Microsoft.AspNet.Testing.Tests
{
    public class OSSkipConditionAttributeTest
    {
        [Fact]
        public void Throws_OnUnrecognizedOperatingSystem()
        {
            // Arrange
            var osName = "Blah";
            var env = new MockRuntimeEnvironment(osName, "2.5");

            // Act
            var osSkipAttribute = new OSSkipConditionAttribute(OperatingSystems.Windows, env);

            // Assert
            var exception = Assert.Throws<InvalidOperationException>(() => osSkipAttribute.IsMet);
            Assert.Equal($"Unrecognized operating system '{osName}'.", exception.Message);
        }

        [Fact]
        public void Skips_WhenOnlyOperatingSystemIsSupplied()
        {
            // Arrange
            var env = new MockRuntimeEnvironment("Windows", "2.5");

            // Act
            var osSkipAttribute = new OSSkipConditionAttribute(OperatingSystems.Windows, env);

            // Assert
            Assert.False(osSkipAttribute.IsMet);
        }

        [Fact]
        public void DoesNotSkip_WhenOperatingSystemDoesNotMatch()
        {
            // Arrange
            var env = new MockRuntimeEnvironment("Windows", "2.5");

            // Act
            var osSkipAttribute = new OSSkipConditionAttribute(OperatingSystems.Linux, env);

            // Assert
            Assert.True(osSkipAttribute.IsMet);
        }

        [Fact]
        public void DoesNotSkip_WhenVersionsDoNotMatch()
        {
            // Arrange
            var env = new MockRuntimeEnvironment("Windows", "2.5");

            // Act
            var osSkipAttribute = new OSSkipConditionAttribute(OperatingSystems.Windows, env, "10.0");

            // Assert
            Assert.True(osSkipAttribute.IsMet);
        }

        [Fact]
        public void DoesNotSkip_WhenOnlyVersionsMatch()
        {
            // Arrange
            var env = new MockRuntimeEnvironment("Windows", "2.5");

            // Act
            var osSkipAttribute = new OSSkipConditionAttribute(OperatingSystems.Linux, env, "2.5");

            // Assert
            Assert.True(osSkipAttribute.IsMet);
        }

        [Theory]
        [InlineData("2.5", "2.5")]
        [InlineData("blue", "Blue")]
        public void Skips_WhenVersionsMatches(string currentOSVersion, string skipVersion)
        {
            // Arrange
            var env = new MockRuntimeEnvironment("Windows", currentOSVersion);

            // Act
            var osSkipAttribute = new OSSkipConditionAttribute(OperatingSystems.Windows, env, skipVersion);

            // Assert
            Assert.False(osSkipAttribute.IsMet);
        }

        [Fact]
        public void Skips_WhenVersionsMatchesOutOfMultiple()
        {
            // Arrange
            var env = new MockRuntimeEnvironment("Windows", "2.5");

            // Act
            var osSkipAttribute = new OSSkipConditionAttribute(OperatingSystems.Windows, env, "10.0", "3.4", "2.5");

            // Assert
            Assert.False(osSkipAttribute.IsMet);
        }

        private class MockRuntimeEnvironment : IRuntimeEnvironment
        {
            public MockRuntimeEnvironment(string operatingSystem, string version)
            {
                OperatingSystem = operatingSystem;
                OperatingSystemVersion = version;
            }

            public string OperatingSystem { get; }

            public string OperatingSystemVersion { get; }

            public string RuntimeArchitecture
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public string RuntimePath
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public string RuntimeType
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public string RuntimeVersion
            {
                get
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
