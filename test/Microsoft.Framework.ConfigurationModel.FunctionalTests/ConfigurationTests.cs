// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.Framework.ConfigurationModel
{
    public class ConfigurationTests : IDisposable
    {
        private string _iniConfigFilePath;
        private string _xmlConfigFilePath;
        private string _jsonConfigFilePath;
        private static readonly string _iniConfigFileContent =
            @"IniKey1=IniValue1
[IniKey2]
# Comments
IniKey3=IniValue2
; Comments
IniKey4=IniValue3
IniKey5:IniKey6=IniValue4
/Comments
[CommonKey1:CommonKey2]
IniKey7=IniValue5
CommonKey3:CommonKey4=IniValue6";
        private static readonly string _xmlConfigFileContent =
            @"<settings XmlKey1=""XmlValue1"">
  <!-- Comments -->
  <XmlKey2 XmlKey3=""XmlValue2"">
    <!-- Comments -->
    <XmlKey4>XmlValue3</XmlKey4>
    <XmlKey5 Name=""XmlKey6"">XmlValue4</XmlKey5>
  </XmlKey2>
  <CommonKey1 Name=""CommonKey2"" XmlKey7=""XmlValue5"">
    <!-- Comments -->
    <CommonKey3 CommonKey4=""XmlValue6"" />
  </CommonKey1>
</settings>";
        private static readonly string _jsonConfigFileContent =
            @"{
  ""JsonKey1"": ""JsonValue1"",
  ""JsonKey2"": {
    ""JsonKey3"": ""JsonValue2"",
    ""JsonKey4"": ""JsonValue3"",
    ""JsonKey5:JsonKey6"": ""JsonValue4""
  },
  ""CommonKey1"": {
    ""CommonKey2"": {
      ""JsonKey7"": ""JsonValue5"",
      ""CommonKey3:CommonKey4"": ""JsonValue6""
    }
  }
}";
        private static readonly Dictionary<string, string> _memConfigContent = new Dictionary<string, string>
            {
                { "MemKey1", "MemValue1" },
                { "MemKey2:MemKey3", "MemValue2" },
                { "MemKey2:MemKey4", "MemValue3" },
                { "MemKey2:MemKey5:MemKey6", "MemValue4" },
                { "CommonKey1:CommonKey2:MemKey7", "MemValue5" },
                { "CommonKey1:CommonKey2:CommonKey3:CommonKey4", "MemValue6" }
            };

        public ConfigurationTests()
        {
            _iniConfigFilePath = Path.GetTempFileName();
            _xmlConfigFilePath = Path.GetTempFileName();
            _jsonConfigFilePath = Path.GetTempFileName();

            File.WriteAllText(_iniConfigFilePath, _iniConfigFileContent);
            File.WriteAllText(_xmlConfigFilePath, _xmlConfigFileContent);
            File.WriteAllText(_jsonConfigFilePath, _jsonConfigFileContent);
        }

        [Fact]
        public void LoadAndCombineKeyValuePairsFromDifferentConfigurationSources()
        {
            // Arrange
            var config = new Configuration();

            // Act
            config.AddIniFile(_iniConfigFilePath);
            config.AddJsonFile(_jsonConfigFilePath);
            config.AddXmlFile(_xmlConfigFilePath);
            var memConfigSrc = new MemoryConfigurationSource(_memConfigContent);
            config.Add(memConfigSrc);

            // Assert
            Assert.Equal(24, CountAllEntries(config));

            Assert.Equal("IniValue1", config.Get("IniKey1"));
            Assert.Equal("IniValue2", config.Get("IniKey2:IniKey3"));
            Assert.Equal("IniValue3", config.Get("IniKey2:IniKey4"));
            Assert.Equal("IniValue4", config.Get("IniKey2:IniKey5:IniKey6"));
            Assert.Equal("IniValue5", config.Get("CommonKey1:CommonKey2:IniKey7"));

            Assert.Equal("JsonValue1", config.Get("JsonKey1"));
            Assert.Equal("JsonValue2", config.Get("JsonKey2:JsonKey3"));
            Assert.Equal("JsonValue3", config.Get("JsonKey2:JsonKey4"));
            Assert.Equal("JsonValue4", config.Get("JsonKey2:JsonKey5:JsonKey6"));
            Assert.Equal("JsonValue5", config.Get("CommonKey1:CommonKey2:JsonKey7"));

            Assert.Equal("XmlValue1", config.Get("XmlKey1"));
            Assert.Equal("XmlValue2", config.Get("XmlKey2:XmlKey3"));
            Assert.Equal("XmlValue3", config.Get("XmlKey2:XmlKey4"));
            Assert.Equal("XmlValue4", config.Get("XmlKey2:XmlKey5:XmlKey6"));
            Assert.Equal("XmlValue5", config.Get("CommonKey1:CommonKey2:XmlKey7"));

            Assert.Equal("MemValue1", config.Get("MemKey1"));
            Assert.Equal("MemValue2", config.Get("MemKey2:MemKey3"));
            Assert.Equal("MemValue3", config.Get("MemKey2:MemKey4"));
            Assert.Equal("MemValue4", config.Get("MemKey2:MemKey5:MemKey6"));
            Assert.Equal("MemValue5", config.Get("CommonKey1:CommonKey2:MemKey7"));

            Assert.Equal("MemValue6", config.Get("CommonKey1:CommonKey2:CommonKey3:CommonKey4"));
        }

        [Fact]
        public void CanOverrideValuesWithNewConfigurationSource()
        {
            // Arrange
            var config = new Configuration();

            // Act & Assert
            config.AddIniFile(_iniConfigFilePath);
            Assert.Equal("IniValue6", config.Get("CommonKey1:CommonKey2:CommonKey3:CommonKey4"));

            config.AddJsonFile(_jsonConfigFilePath);
            Assert.Equal("JsonValue6", config.Get("CommonKey1:CommonKey2:CommonKey3:CommonKey4"));

            config.AddXmlFile(_xmlConfigFilePath);
            Assert.Equal("XmlValue6", config.Get("CommonKey1:CommonKey2:CommonKey3:CommonKey4"));

            var memConfigSrc = new MemoryConfigurationSource(_memConfigContent);
            config.Add(memConfigSrc);
            Assert.Equal("MemValue6", config.Get("CommonKey1:CommonKey2:CommonKey3:CommonKey4"));
        }

        [Fact]
        public void CanSetValuesAndReloadValues()
        {
            // Arrange
            var config = new Configuration();
            config.AddIniFile(_iniConfigFilePath);
            config.AddJsonFile(_jsonConfigFilePath);
            config.AddXmlFile(_xmlConfigFilePath);

            // Act & Assert
            config.Set("CommonKey1:CommonKey2:CommonKey3:CommonKey4", "NewValue");

            // All config sources must be updated
            foreach (var src in config)
            {
                Assert.Equal("NewValue",
                    (src as BaseConfigurationSource).Data["CommonKey1:CommonKey2:CommonKey3:CommonKey4"]);
            }

            // Recover values by reloading
            config.Reload();
            Assert.Equal("XmlValue6", config.Get("CommonKey1:CommonKey2:CommonKey3:CommonKey4"));
        }

        [Fact]
        public void CanCommitChangeBackToLastConfigFile()
        {
            // Arrange
            var config = new Configuration();
            config.AddIniFile(_iniConfigFilePath);
            config.AddJsonFile(_jsonConfigFilePath);
            config.AddXmlFile(_xmlConfigFilePath);
            config.Set("CommonKey1:CommonKey2:CommonKey3:CommonKey4", "NewValue");

            // Act
            config.Commit();

            // Assert
            Assert.Equal(_iniConfigFileContent, File.ReadAllText(_iniConfigFilePath));

            Assert.Equal(_jsonConfigFileContent, File.ReadAllText(_jsonConfigFilePath));

            var updatedXmlContent = _xmlConfigFileContent.Replace("XmlValue6", "NewValue");
            Assert.Equal(updatedXmlContent, File.ReadAllText(_xmlConfigFilePath));
        }

        private static int CountAllEntries(Configuration config)
        {
            return config.Aggregate(0, (acc, src) => acc + (src as BaseConfigurationSource).Data.Count);
        }

        public void Dispose()
        {
            File.Delete(_iniConfigFilePath);
            File.Delete(_xmlConfigFilePath);
            File.Delete(_jsonConfigFilePath);
        }
    }
}
