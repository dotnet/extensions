// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.Framework.ConfigurationModel
{
    public class CommandLineConfigurationSourceTest
    {
        [Fact]
        public void LoadKeyValuePairsFromCommandLineArgumentsWithoutSwitchMappings()
        {
            var args = new string[]
                {
                    "Key1=Value1",
                    "--Key2=Value2",
                    "/Key3=Value3",
                    "--Key4", "Value4",
                    "/Key5", "Value5"
                };
            var cmdLineConfig = new CommandLineConfigurationSource(args);

            cmdLineConfig.Load();

            Assert.Equal(5, cmdLineConfig.Data.Count);
            Assert.Equal("Value1", cmdLineConfig.Data["Key1"]);
            Assert.Equal("Value2", cmdLineConfig.Data["Key2"]);
            Assert.Equal("Value3", cmdLineConfig.Data["Key3"]);
            Assert.Equal("Value4", cmdLineConfig.Data["Key4"]);
            Assert.Equal("Value5", cmdLineConfig.Data["Key5"]);
        }

        [Fact]
        public void LoadKeyValuePairsFromCommandLineArgumentsWithSwitchMappings()
        {
            var args = new string[]
                {
                    "-K1=Value1",
                    "--Key2=Value2",
                    "/Key3=Value3",
                    "--Key4", "Value4",
                    "/Key5", "Value5",
                    "/Key6=Value6"
                };
            var switchMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "-K1", "LongKey1" },
                    { "--Key2", "SuperLongKey2" },
                    { "--Key6", "SuchALongKey6"}
                };
            var cmdLineConfig = new CommandLineConfigurationSource(args, switchMappings);

            cmdLineConfig.Load();

            Assert.Equal(6, cmdLineConfig.Data.Count);
            Assert.Equal("Value1", cmdLineConfig.Data["LongKey1"]);
            Assert.Equal("Value2", cmdLineConfig.Data["SuperLongKey2"]);
            Assert.Equal("Value3", cmdLineConfig.Data["Key3"]);
            Assert.Equal("Value4", cmdLineConfig.Data["Key4"]);
            Assert.Equal("Value5", cmdLineConfig.Data["Key5"]);
            Assert.Equal("Value6", cmdLineConfig.Data["SuchALongKey6"]);
        }

        [Fact]
        public void ThrowExceptionWhenPassingSwitchMappingsWithDuplicatedKeys()
        {
            // Arrange
            var args = new string[]
                {
                    "-K1=Value1",
                    "--Key2=Value2",
                    "/Key3=Value3",
                    "--Key4", "Value4",
                    "/Key5", "Value5"
                };
            var switchMappings = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "--KEY1", "LongKey1" },
                    { "--key1", "SuperLongKey1" },
                    { "-Key2", "LongKey2" },
                    { "-KEY2", "LongKey2"}
                };

            // Find out the duplicate expected be be reported
            var expectedDup = string.Empty;
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var mapping in switchMappings)
            {
                if (set.Contains(mapping.Key))
                {
                    expectedDup = mapping.Key;
                    break;
                }

                set.Add(mapping.Key);
            }

            var expectedMsg = new ArgumentException(Resources.
                FormatError_DuplicatedKeyInSwitchMappings(expectedDup), "switchMappings").Message;

            // Act
            var exception = Assert.Throws<ArgumentException>(
                () => new CommandLineConfigurationSource(args, switchMappings));

            // Assert
            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenSwitchMappingsContainInvalidKey()
        {
            var args = new string[]
                {
                    "-K1=Value1",
                    "--Key2=Value2",
                    "/Key3=Value3",
                    "--Key4", "Value4",
                    "/Key5", "Value5"
                };
            var switchMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "-K1", "LongKey1" },
                    { "--Key2", "SuperLongKey2" },
                    { "/Key3", "AnotherSuperLongKey3" }
                };
            var expectedMsg = new ArgumentException(Resources.FormatError_InvalidSwitchMapping("/Key3"),
                "switchMappings").Message;

            var exception = Assert.Throws<ArgumentException>(
                () => new CommandLineConfigurationSource(args, switchMappings));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenNullIsPassedToConstructorAsArgs()
        {
            string[] args = null;
            var expectedMsg = new ArgumentNullException("args").Message;

            var exception = Assert.Throws<ArgumentNullException>(() => new CommandLineConfigurationSource(args));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenKeyIsDuplicated()
        {
            var args = new string[]
                {
                    "/Key1=Value1",
                    "--Key1=Value2"
                };
            var expectedMsg = new FormatException(Resources.FormatError_KeyIsDuplicated("Key1")).Message;
            var cmdLineConfig = new CommandLineConfigurationSource(args);

            var exception = Assert.Throws<FormatException>(() => cmdLineConfig.Load());

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenValueForAKeyIsMissing()
        {
            var args = new string[]
                {
                    "--Key1", "Value1",
                    "/Key2" /* The value for Key2 is missing here */
                };
            var expectedMsg = new FormatException(Resources.FormatError_ValueIsMissing("/Key2")).Message;
            var cmdLineConfig = new CommandLineConfigurationSource(args);

            var exception = Assert.Throws<FormatException>(() => cmdLineConfig.Load());

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenAnArgumentCannotBeRecognized()
        {
            var args = new string[]
                {
                    "ArgWithoutPrefixAndEqualSign"
                };
            var expectedMsg = new FormatException(
                Resources.FormatError_UnrecognizedArgumentFormat("ArgWithoutPrefixAndEqualSign")).Message;
            var cmdLineConfig = new CommandLineConfigurationSource(args);

            var exception = Assert.Throws<FormatException>(() => cmdLineConfig.Load());

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenShortSwitchNotDefined()
        {
            var args = new string[]
                {
                    "-Key1", "Value1",
                };
            var switchMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "-Key2", "LongKey2" }
                };
            var expectedMsg = new FormatException(Resources.FormatError_ShortSwitchNotDefined("-Key1")).Message;
            var cmdLineConfig = new CommandLineConfigurationSource(args, switchMappings);

            var exception = Assert.Throws<FormatException>(() => cmdLineConfig.Load());

            Assert.Equal(expectedMsg, exception.Message);
        }
    }
}
