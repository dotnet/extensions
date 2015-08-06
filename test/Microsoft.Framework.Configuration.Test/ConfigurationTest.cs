// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.Configuration.Memory;
using Xunit;

namespace Microsoft.Framework.Configuration.Test
{
    public class ConfigurationTest
    {
        [Fact]
        public void SetBasePathThroughConstructor()
        {
            var expectedBasePath = @"C:\ExamplePath";
            var builder = new ConfigurationBuilder(basePath: expectedBasePath);

            Assert.Equal(expectedBasePath, builder.BasePath);
        }

        [Fact]
        public void DefaultBasePathIsNull()
        {
            var builder = new ConfigurationBuilder();

            Assert.Null(builder.BasePath);
        }

        [Fact]
        public void LoadAndCombineKeyValuePairsFromDifferentConfigurationSources()
        {
            // Arrange
            var dic1 = new Dictionary<string, string>()
                {
                    {"Mem1:KeyInMem1", "ValueInMem1"}
                };
            var dic2 = new Dictionary<string, string>()
                {
                    {"Mem2:KeyInMem2", "ValueInMem2"}
                };
            var dic3 = new Dictionary<string, string>()
                {
                    {"Mem3:KeyInMem3", "ValueInMem3"}
                };
            var memConfigSrc1 = new MemoryConfigurationSource(dic1);
            var memConfigSrc2 = new MemoryConfigurationSource(dic2);
            var memConfigSrc3 = new MemoryConfigurationSource(dic3);

            var builder = new ConfigurationBuilder();

            string memVal1, memVal2, memVal3;
            bool memRet1, memRet2, memRet3;

            // Act
            builder.Add(memConfigSrc1, load: false);
            builder.Add(memConfigSrc2, load: false);
            builder.Add(memConfigSrc3, load: false);

            var config = builder.Build();

            memVal1 = config["mem1:keyinmem1"];
            memVal2 = config["Mem2:KeyInMem2"];
            memVal3 = config["MEM3:KEYINMEM3"];

            // Assert
            Assert.Contains(memConfigSrc1, builder.Sources);
            Assert.Contains(memConfigSrc2, builder.Sources);
            Assert.Contains(memConfigSrc3, builder.Sources);

            Assert.Equal("ValueInMem1", memVal1);
            Assert.Equal("ValueInMem2", memVal2);
            Assert.Equal("ValueInMem3", memVal3);

            Assert.Equal("ValueInMem1", config["mem1:keyinmem1"]);
            Assert.Equal("ValueInMem2", config["Mem2:KeyInMem2"]);
            Assert.Equal("ValueInMem3", config["MEM3:KEYINMEM3"]);
            Assert.Null(config["NotExist"]);
        }

        [Fact]
        public void NewConfigurationSourceOverridesOldOneWhenKeyIsDuplicated()
        {
            // Arrange
            var dic1 = new Dictionary<string, string>()
                {
                    {"Key1:Key2", "ValueInMem1"}
                };
            var dic2 = new Dictionary<string, string>()
                {
                    {"Key1:Key2", "ValueInMem2"}
                };
            var memConfigSrc1 = new MemoryConfigurationSource(dic1);
            var memConfigSrc2 = new MemoryConfigurationSource(dic2);

            var builder = new ConfigurationBuilder();

            // Act
            builder.Add(memConfigSrc1, load: false);
            builder.Add(memConfigSrc2, load: false);

            var config = builder.Build();

            // Assert
            Assert.Equal("ValueInMem2", config["Key1:Key2"]);
        }

        [Fact]
        public void SettingValueUpdatesAllConfigurationSources()
        {
            // Arrange
            var dict = new Dictionary<string, string>()
                {
                    {"Key1", "Value1"},
                    {"Key2", "Value2"}
                };
            var memConfigSrc1 = new MemoryConfigurationSource(dict);
            var memConfigSrc2 = new MemoryConfigurationSource(dict);
            var memConfigSrc3 = new MemoryConfigurationSource(dict);

            var builder = new ConfigurationBuilder();

            builder.Add(memConfigSrc1, load: false);
            builder.Add(memConfigSrc2, load: false);
            builder.Add(memConfigSrc3, load: false);

            var config = builder.Build();

            // Act
            config["Key1"] =  "NewValue1";
            config["Key2"] = "NewValue2";

            // Assert
            Assert.Equal("NewValue1", config["Key1"]);
            Assert.Equal("NewValue1", memConfigSrc1.Get("Key1"));
            Assert.Equal("NewValue1", memConfigSrc2.Get("Key1"));
            Assert.Equal("NewValue1", memConfigSrc3.Get("Key1"));
            Assert.Equal("NewValue2", config["Key2"]);
            Assert.Equal("NewValue2", memConfigSrc1.Get("Key2"));
            Assert.Equal("NewValue2", memConfigSrc2.Get("Key2"));
            Assert.Equal("NewValue2", memConfigSrc3.Get("Key2"));
        }

        [Fact]
        public void CanGetConfigurationSection()
        {
            // Arrange
            var dic1 = new Dictionary<string, string>()
                {
                    {"Data:DB1:Connection1", "MemVal1"},
                    {"Data:DB1:Connection2", "MemVal2"}
                };
            var dic2 = new Dictionary<string, string>()
                {
                    {"DataSource:DB2:Connection", "MemVal3"}
                };
            var dic3 = new Dictionary<string, string>()
                {
                    {"Data", "MemVal4"}
                };
            var memConfigSrc1 = new MemoryConfigurationSource(dic1);
            var memConfigSrc2 = new MemoryConfigurationSource(dic2);
            var memConfigSrc3 = new MemoryConfigurationSource(dic3);

            var builder = new ConfigurationBuilder();
            builder.Add(memConfigSrc1, load: false);
            builder.Add(memConfigSrc2, load: false);
            builder.Add(memConfigSrc3, load: false);

            var config = builder.Build();

            string memVal1, memVal2, memVal3, memVal4, memVal5;

            // Act
            var configFocus = config.GetSection("Data");

            memVal1 = configFocus["DB1:Connection1"];
            memVal2 = configFocus["DB1:Connection2"];
            memVal3 = configFocus["DB2:Connection"];
            memVal4 = configFocus["Source:DB2:Connection"];
            memVal5 = configFocus.Value;

            // Assert
            Assert.Equal("MemVal1", memVal1);
            Assert.Equal("MemVal2", memVal2);
            Assert.Equal("MemVal4", memVal5);

            Assert.Equal("MemVal1", configFocus["DB1:Connection1"]);
            Assert.Equal("MemVal2", configFocus["DB1:Connection2"]);
            Assert.Null(configFocus["DB2:Connection"]);
            Assert.Null(configFocus["Source:DB2:Connection"]);
            Assert.Equal("MemVal4", configFocus.Value);
        }

        [Fact]
        public void CanGetConfigurationSections()
        {
            // Arrange
            var dic1 = new Dictionary<string, string>()
                {
                    {"Data:DB1:Connection1", "MemVal1"},
                    {"Data:DB1:Connection2", "MemVal2"}
                };
            var dic2 = new Dictionary<string, string>()
                {
                    {"Data:DB2Connection", "MemVal3"}
                };
            var dic3 = new Dictionary<string, string>()
                {
                    {"DataSource:DB3:Connection", "MemVal4"}
                };
            var memConfigSrc1 = new MemoryConfigurationSource(dic1);
            var memConfigSrc2 = new MemoryConfigurationSource(dic2);
            var memConfigSrc3 = new MemoryConfigurationSource(dic3);

            var builder = new ConfigurationBuilder();
            builder.Add(memConfigSrc1, load: false);
            builder.Add(memConfigSrc2, load: false);
            builder.Add(memConfigSrc3, load: false);

            var config = builder.Build();

            // Act
            var configSections = config.GetSection("Data").GetChildren().ToList();

            // Assert
            Assert.Equal(2, configSections.Count());
            Assert.Equal("MemVal1", configSections.FirstOrDefault(c => c.Key == "Data:DB1")["Connection1"]);
            Assert.Equal("MemVal2", configSections.FirstOrDefault(c => c.Key == "Data:DB1")["Connection2"]);
            Assert.Equal("MemVal3", configSections.FirstOrDefault(c => c.Key == "Data:DB2Connection").Value);
            Assert.False(configSections.Exists(c => c.Key == "Data:DB3"));
            Assert.False(configSections.Exists(c => c.Key == "Source:DB3"));
        }

        [Fact]
        public void CanIterateWithGenericEnumerator()
        {
            // Arrange
            var dict = new Dictionary<string, string>()
                {
                    {"Mem:KeyInMem", "MemVal"}
                };
            var memConfigSrc1 = new MemoryConfigurationSource(dict);
            var memConfigSrc2 = new MemoryConfigurationSource(dict);
            var memConfigSrc3 = new MemoryConfigurationSource(dict);

            var srcSet = new HashSet<IConfigurationSource>()
                {
                    memConfigSrc1,
                    memConfigSrc2,
                    memConfigSrc3
                };

            var builder = new ConfigurationBuilder();

            // Act
            builder.Add(memConfigSrc1, load: false);
            builder.Add(memConfigSrc2, load: false);
            builder.Add(memConfigSrc3, load: false);

            var config = builder.Build();

            // Assert
            var enumerator = builder.Sources.GetEnumerator();
            int srcCount = 0;
            while (enumerator.MoveNext())
            {
                Assert.Contains(enumerator.Current, srcSet);
                ++srcCount;
            }

            Assert.Equal(3, srcCount);
        }

        [Fact]
        public void CanIterateAfterCastedToIEnumerable()
        {
            // Arrange
            var dict = new Dictionary<string, string>()
                {
                    {"Mem:KeyInMem", "MemVal"}
                };
            var memConfigSrc1 = new MemoryConfigurationSource(dict);
            var memConfigSrc2 = new MemoryConfigurationSource(dict);
            var memConfigSrc3 = new MemoryConfigurationSource(dict);

            var srcSet = new HashSet<IConfigurationSource>()
                {
                    memConfigSrc1,
                    memConfigSrc2,
                    memConfigSrc3
                };

            var builder = new ConfigurationBuilder();

            // Act
            builder.Add(memConfigSrc1, load: false);
            builder.Add(memConfigSrc2, load: false);
            builder.Add(memConfigSrc3, load: false);

            var enumerable = builder as IEnumerable;

            // Assert
            var enumerator = builder.Sources.GetEnumerator();
            int srcCount = 0;
            while (enumerator.MoveNext())
            {
                Assert.Contains(enumerator.Current, srcSet);
                ++srcCount;
            }

            Assert.Equal(3, srcCount);
        }

        [Fact]
        public void SetValueThrowsExceptionNoSourceRegistered()
        {
            // Arrange
            var builder = new ConfigurationBuilder();
            var config = builder.Build();

            var expectedMsg = Resources.Error_NoSources;

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => config["Title"] = "Welcome");

            // Assert
            Assert.Equal(expectedMsg, ex.Message);
        }
    }
}
