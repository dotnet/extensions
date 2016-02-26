// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration.Memory;
using Xunit;

namespace Microsoft.Extensions.Configuration.Test
{
    public class ConfigurationTest
    {
        [Fact]
        public void CanChainConfiguration()
        {
            // Arrange
            var dic1 = new Dictionary<string, string>()
                {
                    {"Mem1:KeyInMem1", "ValueInMem1"}
                };
            var memConfigSrc1 = new MemoryConfigurationProvider(dic1);

            var configurationBuilder = new ConfigurationBuilder();

            // Act
            configurationBuilder.Add(memConfigSrc1, load: false);

            var dic2 = new Dictionary<string, string>()
                {
                    {"Mem2:KeyInMem2", "ValueInMem2"}
                };
            var memConfigSrc2 = new MemoryConfigurationProvider(dic2);
            var configurationBuilder2 = new ConfigurationBuilder();
            configurationBuilder2.Add(memConfigSrc2, load: false);
            configurationBuilder2.Include(configurationBuilder.Build());

            var config = configurationBuilder2.Build();

            var memVal1 = config["mem1:keyinmem1"];
            var memVal2 = config["Mem2:KeyInMem2"];

            // Assert
            Assert.Contains(memConfigSrc2, configurationBuilder2.Providers);
            Assert.True(configurationBuilder2.Providers.ElementAt(1) is IncludedConfigurationProvider);

            Assert.Equal("ValueInMem1", memVal1);
            Assert.Equal("ValueInMem2", memVal2);
            Assert.Null(config["NotExist"]);
        }

        [Fact]
        public void CanIncludeConfigurationWithPrefix()
        {
            // Arrange
            var dic1 = new Dictionary<string, string>()
            {
                {"Data:Key", "Value"},
                {"Data:DB1:Connection1", "MemVal1"},
                {"Data:DB1:Connection2", "MemVal2"}
            };
            var memConfigSrc1 = new MemoryConfigurationProvider(dic1);

            var configurationBuilder = new ConfigurationBuilder();

            // Act
            configurationBuilder.Add(memConfigSrc1, load: false);

            var dic2 = new Dictionary<string, string>()
            {
                {"Mem2:KeyInMem2", "MemVal3"}
            };
            var memConfigSrc2 = new MemoryConfigurationProvider(dic2);
            var configurationBuilder2 = new ConfigurationBuilder();
            configurationBuilder2.Add(memConfigSrc2, load: false);
            configurationBuilder2.Include("Data", configurationBuilder.Build());

            var config = configurationBuilder2.Build();

            var memVal0 = config["Key"];
            var memVal1 = config["DB1:Connection1"];
            var memVal2 = config["DB1:Connection2"];
            var memVal3 = config["Mem2:KeyInMem2"];

            // Assert
            Assert.Contains(memConfigSrc2, configurationBuilder2.Providers);
            Assert.True(configurationBuilder2.Providers.ElementAt(1) is IncludedConfigurationProvider);

            Assert.Equal("Value", memVal0);
            Assert.Equal("MemVal1", memVal1);
            Assert.Equal("MemVal2", memVal2);
            Assert.Equal("MemVal3", memVal3);

            Assert.Null(config["NotExist"]);
        }

        [Fact]
        public void LoadAndCombineKeyValuePairsFromDifferentConfigurationProviders()
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
            var memConfigSrc1 = new MemoryConfigurationProvider(dic1);
            var memConfigSrc2 = new MemoryConfigurationProvider(dic2);
            var memConfigSrc3 = new MemoryConfigurationProvider(dic3);

            var configurationBuilder = new ConfigurationBuilder();

            // Act
            configurationBuilder.Add(memConfigSrc1, load: false);
            configurationBuilder.Add(memConfigSrc2, load: false);
            configurationBuilder.Add(memConfigSrc3, load: false);

            var config = configurationBuilder.Build();

            var memVal1 = config["mem1:keyinmem1"];
            var memVal2 = config["Mem2:KeyInMem2"];
            var memVal3 = config["MEM3:KEYINMEM3"];

            // Assert
            Assert.Contains(memConfigSrc1, configurationBuilder.Providers);
            Assert.Contains(memConfigSrc2, configurationBuilder.Providers);
            Assert.Contains(memConfigSrc3, configurationBuilder.Providers);

            Assert.Equal("ValueInMem1", memVal1);
            Assert.Equal("ValueInMem2", memVal2);
            Assert.Equal("ValueInMem3", memVal3);

            Assert.Equal("ValueInMem1", config["mem1:keyinmem1"]);
            Assert.Equal("ValueInMem2", config["Mem2:KeyInMem2"]);
            Assert.Equal("ValueInMem3", config["MEM3:KEYINMEM3"]);
            Assert.Null(config["NotExist"]);
        }

        [Fact]
        public void AsEnumerateFlattensIntoDictionaryTest()
        {
            // Arrange
            var dic1 = new Dictionary<string, string>()
            {
                {"Mem1:KeyInMem1", "ValueInMem1"},
                {"Mem1:KeyInMem1:Deep1", "ValueDeep1"}
            };
            var dic2 = new Dictionary<string, string>()
            {
                {"Mem2:KeyInMem2", "ValueInMem2"},
                {"Mem2:KeyInMem2:Deep2", "ValueDeep2"}
            };
            var dic3 = new Dictionary<string, string>()
            {
                {"Mem3:KeyInMem3", "ValueInMem3"},
                {"Mem3:KeyInMem3:Deep3", "ValueDeep3"}
            };
            var memConfigSrc1 = new MemoryConfigurationProvider(dic1);
            var memConfigSrc2 = new MemoryConfigurationProvider(dic2);
            var memConfigSrc3 = new MemoryConfigurationProvider(dic3);

            var configurationBuilder = new ConfigurationBuilder();

            // Act
            configurationBuilder.Add(memConfigSrc1, load: false);
            configurationBuilder.Add(memConfigSrc2, load: false);
            configurationBuilder.Add(memConfigSrc3, load: false);

            var config = configurationBuilder.Build();

            var dict = config.AsEnumerable().ToDictionary(k => k.Key, v => v.Value);
            Assert.Equal("ValueInMem1", config["Mem1:KeyInMem1"]);
            Assert.Equal("ValueDeep1", config["Mem1:KeyInMem1:Deep1"]);
            Assert.Equal("ValueInMem2", config["Mem2:KeyInMem2"]);
            Assert.Equal("ValueDeep2", config["Mem2:KeyInMem2:Deep2"]);
            Assert.Equal("ValueInMem3", config["Mem3:KeyInMem3"]);
            Assert.Equal("ValueDeep3", config["Mem3:KeyInMem3:Deep3"]);
        }



        [Fact]
        public void NewConfigurationProviderOverridesOldOneWhenKeyIsDuplicated()
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
            var memConfigSrc1 = new MemoryConfigurationProvider(dic1);
            var memConfigSrc2 = new MemoryConfigurationProvider(dic2);

            var configurationBuilder = new ConfigurationBuilder();

            // Act
            configurationBuilder.Add(memConfigSrc1, load: false);
            configurationBuilder.Add(memConfigSrc2, load: false);

            var config = configurationBuilder.Build();

            // Assert
            Assert.Equal("ValueInMem2", config["Key1:Key2"]);
        }

        [Fact]
        public void SettingValueUpdatesAllConfigurationProviders()
        {
            // Arrange
            var dict = new Dictionary<string, string>()
                {
                    {"Key1", "Value1"},
                    {"Key2", "Value2"}
                };
            var memConfigSrc1 = new MemoryConfigurationProvider(dict);
            var memConfigSrc2 = new MemoryConfigurationProvider(dict);
            var memConfigSrc3 = new MemoryConfigurationProvider(dict);

            var configurationBuilder = new ConfigurationBuilder();

            configurationBuilder.Add(memConfigSrc1, load: false);
            configurationBuilder.Add(memConfigSrc2, load: false);
            configurationBuilder.Add(memConfigSrc3, load: false);

            var config = configurationBuilder.Build();

            // Act
            config["Key1"] = "NewValue1";
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
            var memConfigSrc1 = new MemoryConfigurationProvider(dic1);
            var memConfigSrc2 = new MemoryConfigurationProvider(dic2);
            var memConfigSrc3 = new MemoryConfigurationProvider(dic3);

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.Add(memConfigSrc1, load: false);
            configurationBuilder.Add(memConfigSrc2, load: false);
            configurationBuilder.Add(memConfigSrc3, load: false);

            var config = configurationBuilder.Build();

            // Act
            var configFocus = config.GetSection("Data");

            var memVal1 = configFocus["DB1:Connection1"];
            var memVal2 = configFocus["DB1:Connection2"];
            var memVal3 = configFocus["DB2:Connection"];
            var memVal4 = configFocus["Source:DB2:Connection"];
            var memVal5 = configFocus.Value;

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
        public void CanGetConfigurationChildren()
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
            var memConfigSrc1 = new MemoryConfigurationProvider(dic1);
            var memConfigSrc2 = new MemoryConfigurationProvider(dic2);
            var memConfigSrc3 = new MemoryConfigurationProvider(dic3);

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.Add(memConfigSrc1, load: false);
            configurationBuilder.Add(memConfigSrc2, load: false);
            configurationBuilder.Add(memConfigSrc3, load: false);

            var config = configurationBuilder.Build();

            // Act
            var configSections = config.GetSection("Data").GetChildren().ToList();

            // Assert
            Assert.Equal(2, configSections.Count());
            Assert.Equal("MemVal1", configSections.FirstOrDefault(c => c.Key == "DB1")["Connection1"]);
            Assert.Equal("MemVal2", configSections.FirstOrDefault(c => c.Key == "DB1")["Connection2"]);
            Assert.Equal("MemVal3", configSections.FirstOrDefault(c => c.Key == "DB2Connection").Value);
            Assert.False(configSections.Exists(c => c.Key == "DB3"));
            Assert.False(configSections.Exists(c => c.Key == "DB3"));
        }

        [Fact]
        public void SourcesReturnsAddedConfigurationProviders()
        {
            // Arrange
            var dict = new Dictionary<string, string>()
            {
                {"Mem:KeyInMem", "MemVal"}
            };
            var memConfigSrc1 = new MemoryConfigurationProvider(dict);
            var memConfigSrc2 = new MemoryConfigurationProvider(dict);
            var memConfigSrc3 = new MemoryConfigurationProvider(dict);

            var srcSet = new HashSet<IConfigurationProvider>()
            {
                memConfigSrc1,
                memConfigSrc2,
                memConfigSrc3
            };

            var configurationBuilder = new ConfigurationBuilder();

            // Act
            configurationBuilder.Add(memConfigSrc1, load: false);
            configurationBuilder.Add(memConfigSrc2, load: false);
            configurationBuilder.Add(memConfigSrc3, load: false);

            var config = configurationBuilder.Build();

            // Assert
            Assert.Equal(new[] { memConfigSrc1, memConfigSrc2, memConfigSrc3 }, configurationBuilder.Providers);
        }

        [Fact]
        public void SetValueThrowsExceptionNoSourceRegistered()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();
            var config = configurationBuilder.Build();

            var expectedMsg = Resources.Error_NoSources;

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => config["Title"] = "Welcome");

            // Assert
            Assert.Equal(expectedMsg, ex.Message);
        }

        [Fact]
        public void SameReloadTokenIsReturnedRepeatedly()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();
            var config = configurationBuilder.Build();

            // Act
            var token1 = config.GetReloadToken();
            var token2 = config.GetReloadToken();

            // Assert
            Assert.Same(token1, token2);
        }

        [Fact]
        public void DifferentReloadTokenReturnedAfterReloading()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();
            var config = configurationBuilder.Build();

            // Act
            var token1 = config.GetReloadToken();
            var token2 = config.GetReloadToken();
            config.Reload();
            var token3 = config.GetReloadToken();
            var token4 = config.GetReloadToken();

            // Assert
            Assert.Same(token1, token2);
            Assert.Same(token3, token4);
            Assert.NotSame(token1, token3);
        }

        [Fact]
        public void TokenTriggeredWhenReloadOccurs()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();
            var config = configurationBuilder.Build();

            // Act
            var token1 = config.GetReloadToken();
            var hasChanged1 = token1.HasChanged;
            config.Reload();
            var hasChanged2 = token1.HasChanged;

            // Assert
            Assert.False(hasChanged1);
            Assert.True(hasChanged2);
        }

        [Fact]
        public void NewTokenAfterReloadIsNotChanged()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();
            var config = configurationBuilder.Build();

            // Act
            var token1 = config.GetReloadToken();
            var hasChanged1 = token1.HasChanged;
            config.Reload();
            var hasChanged2 = token1.HasChanged;
            var token2 = config.GetReloadToken();
            var hasChanged3 = token2.HasChanged;

            // Assert
            Assert.False(hasChanged1);
            Assert.True(hasChanged2);
            Assert.False(hasChanged3);
            Assert.NotSame(token1, token2);
        }

        [Fact]
        public void KeyStartingWithColonMeansFirstSectionHasEmptyName()
        {
            // Arrange
            var dict = new Dictionary<string, string>
            {
                [":Key2"] = "value"
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dict);
            var config = configurationBuilder.Build();

            // Act
            var children = config.GetChildren().ToArray();

            // Assert
            Assert.Equal(1, children.Length);
            Assert.Equal(string.Empty, children.First().Key);
            Assert.Equal(1, children.First().GetChildren().Count());
            Assert.Equal("Key2", children.First().GetChildren().First().Key);
        }

        [Fact]
        public void KeyWithDoubleColonHasSectionWithEmptyName()
        {
            // Arrange
            var dict = new Dictionary<string, string>
            {
                ["Key1::Key3"] = "value"
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dict);
            var config = configurationBuilder.Build();

            // Act
            var children = config.GetChildren().ToArray();

            // Assert
            Assert.Equal(1, children.Length);
            Assert.Equal("Key1", children.First().Key);
            Assert.Equal(1, children.First().GetChildren().Count());
            Assert.Equal(string.Empty, children.First().GetChildren().First().Key);
            Assert.Equal(1, children.First().GetChildren().First().GetChildren().Count());
            Assert.Equal("Key3", children.First().GetChildren().First().GetChildren().First().Key);
        }

        [Fact]
        public void KeyEndingWithColonMeansLastSectionHasEmptyName()
        {
            // Arrange
            var dict = new Dictionary<string, string>
            {
                ["Key1:"] = "value"
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dict);
            var config = configurationBuilder.Build();

            // Act
            var children = config.GetChildren().ToArray();

            // Assert
            Assert.Equal(1, children.Length);
            Assert.Equal("Key1", children.First().Key);
            Assert.Equal(1, children.First().GetChildren().Count());
            Assert.Equal(string.Empty, children.First().GetChildren().First().Key);
        }
    }
}
