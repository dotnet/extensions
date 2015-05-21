// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

            memRet1 = config.TryGet("mem1:keyinmem1", out memVal1);
            memRet2 = config.TryGet("Mem2:KeyInMem2", out memVal2);
            memRet3 = config.TryGet("MEM3:KEYINMEM3", out memVal3);

            // Assert
            Assert.Contains(memConfigSrc1, builder.Sources);
            Assert.Contains(memConfigSrc2, builder.Sources);
            Assert.Contains(memConfigSrc3, builder.Sources);

            Assert.True(memRet1);
            Assert.True(memRet2);
            Assert.True(memRet3);

            Assert.Equal("ValueInMem1", memVal1);
            Assert.Equal("ValueInMem2", memVal2);
            Assert.Equal("ValueInMem3", memVal3);

            Assert.Equal("ValueInMem1", config.Get("mem1:keyinmem1"));
            Assert.Equal("ValueInMem2", config.Get("Mem2:KeyInMem2"));
            Assert.Equal("ValueInMem3", config.Get("MEM3:KEYINMEM3"));
            Assert.Null(config.Get("NotExist"));

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
            Assert.Equal("ValueInMem2", config.Get("Key1:Key2"));
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
            config.Set("Key1", "NewValue1");
            config["Key2"] = "NewValue2";

            // Assert
            Assert.Equal("NewValue1", config.Get("Key1"));
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
            bool memRet1, memRet2, memRet3, memRet4, memRet5;

            // Act
            var configFocus = config.GetConfigurationSection("Data");

            memRet1 = configFocus.TryGet("DB1:Connection1", out memVal1);
            memRet2 = configFocus.TryGet("DB1:Connection2", out memVal2);
            memRet3 = configFocus.TryGet("DB2:Connection", out memVal3);
            memRet4 = configFocus.TryGet("Source:DB2:Connection", out memVal4);
            memRet5 = configFocus.TryGet(null, out memVal5);

            // Assert
            Assert.True(memRet1);
            Assert.True(memRet2);
            Assert.False(memRet3);
            Assert.False(memRet4);
            Assert.True(memRet5);

            Assert.Equal("MemVal1", memVal1);
            Assert.Equal("MemVal2", memVal2);
            Assert.Equal("MemVal4", memVal5);

            Assert.Equal("MemVal1", configFocus.Get("DB1:Connection1"));
            Assert.Equal("MemVal2", configFocus.Get("DB1:Connection2"));
            Assert.Null(configFocus.Get("DB2:Connection"));
            Assert.Null(configFocus.Get("Source:DB2:Connection"));
            Assert.Equal("MemVal4", configFocus.Get(null));

            Assert.Equal("MemVal1", configFocus["DB1:Connection1"]);
            Assert.Equal("MemVal2", configFocus["DB1:Connection2"]);
            Assert.Null(configFocus["DB2:Connection"]);
            Assert.Null(configFocus["Source:DB2:Connection"]);
            Assert.Equal("MemVal4", configFocus[null]);
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
            var configFocusList = config.GetConfigurationSections("Data");
            var configurationSectionsSet = configFocusList.ToDictionary(e => e.Key, e => e.Value);

            // Assert
            Assert.Equal(2, configFocusList.Count());
            Assert.Equal("MemVal1", configurationSectionsSet["DB1"].Get("Connection1"));
            Assert.Equal("MemVal2", configurationSectionsSet["DB1"].Get("Connection2"));
            Assert.Equal("MemVal3", configurationSectionsSet["DB2Connection"].Get(null));
            Assert.False(configurationSectionsSet.ContainsKey("DB3"));
            Assert.False(configurationSectionsSet.ContainsKey("Source:DB3"));
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
            var ex = Assert.Throws<InvalidOperationException>(() => config.Set("Title", "Welcome"));

            // Assert
            Assert.Equal(expectedMsg, ex.Message);
        }
    }
}
