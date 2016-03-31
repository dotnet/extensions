// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration.Ini;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.Xml;
using Xunit;

namespace Microsoft.Extensions.Configuration.Test
{
    public class ConfigurationTests : IDisposable
    {
        private readonly string _basePath;
        private readonly string _iniConfigFilePath;
        private readonly string _xmlConfigFilePath;
        private readonly string _jsonConfigFilePath;
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
  ""Json.Key2"": {
    ""JsonKey3"": ""JsonValue2"",
    ""Json.Key4"": ""JsonValue3"",
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
#if NET451
            _basePath = AppDomain.CurrentDomain.GetData("APP_CONTEXT_BASE_DIRECTORY") as string ??
                AppDomain.CurrentDomain.BaseDirectory ??
                string.Empty;
#else
            _basePath = AppContext.BaseDirectory ?? string.Empty;
#endif

            _iniConfigFilePath = Path.GetRandomFileName();
            _xmlConfigFilePath = Path.GetRandomFileName();
            _jsonConfigFilePath = Path.GetRandomFileName();

            File.WriteAllText(Path.Combine(_basePath, _iniConfigFilePath), _iniConfigFileContent);
            File.WriteAllText(Path.Combine(_basePath, _xmlConfigFilePath), _xmlConfigFileContent);
            File.WriteAllText(Path.Combine(_basePath, _jsonConfigFilePath), _jsonConfigFileContent);
        }

        [Fact]
        public void LoadAndCombineKeyValuePairsFromDifferentConfigurationProviders()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();

            // Act
            configurationBuilder.AddIniFile(source => source.Path = _iniConfigFilePath);
            configurationBuilder.AddJsonFile(source => source.Path = _jsonConfigFilePath);
            configurationBuilder.AddXmlFile(source => source.Path = _xmlConfigFilePath);
            configurationBuilder.AddInMemoryCollection(_memConfigContent);

            var config = configurationBuilder.Build();

            // Assert
            Assert.Equal("IniValue1", config["IniKey1"]);
            Assert.Equal("IniValue2", config["IniKey2:IniKey3"]);
            Assert.Equal("IniValue3", config["IniKey2:IniKey4"]);
            Assert.Equal("IniValue4", config["IniKey2:IniKey5:IniKey6"]);
            Assert.Equal("IniValue5", config["CommonKey1:CommonKey2:IniKey7"]);

            Assert.Equal("JsonValue1", config["JsonKey1"]);
            Assert.Equal("JsonValue2", config["Json.Key2:JsonKey3"]);
            Assert.Equal("JsonValue3", config["Json.Key2:Json.Key4"]);
            Assert.Equal("JsonValue4", config["Json.Key2:JsonKey5:JsonKey6"]);
            Assert.Equal("JsonValue5", config["CommonKey1:CommonKey2:JsonKey7"]);

            Assert.Equal("XmlValue1", config["XmlKey1"]);
            Assert.Equal("XmlValue2", config["XmlKey2:XmlKey3"]);
            Assert.Equal("XmlValue3", config["XmlKey2:XmlKey4"]);
            Assert.Equal("XmlValue4", config["XmlKey2:XmlKey5:XmlKey6"]);
            Assert.Equal("XmlValue5", config["CommonKey1:CommonKey2:XmlKey7"]);

            Assert.Equal("MemValue1", config["MemKey1"]);
            Assert.Equal("MemValue2", config["MemKey2:MemKey3"]);
            Assert.Equal("MemValue3", config["MemKey2:MemKey4"]);
            Assert.Equal("MemValue4", config["MemKey2:MemKey5:MemKey6"]);
            Assert.Equal("MemValue5", config["CommonKey1:CommonKey2:MemKey7"]);

            Assert.Equal("MemValue6", config["CommonKey1:CommonKey2:CommonKey3:CommonKey4"]);
        }

        [Fact]
        public void CanOverrideValuesWithNewConfigurationProvider()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();

            // Act & Assert
            configurationBuilder.AddIniFile(_iniConfigFilePath);
            var config = configurationBuilder.Build();
            Assert.Equal("IniValue6", config["CommonKey1:CommonKey2:CommonKey3:CommonKey4"]);

            configurationBuilder.AddJsonFile(source => source.Path = _jsonConfigFilePath);
            config = configurationBuilder.Build();
            Assert.Equal("JsonValue6", config["CommonKey1:CommonKey2:CommonKey3:CommonKey4"]);

            configurationBuilder.AddXmlFile(source => source.Path = _xmlConfigFilePath);
            config = configurationBuilder.Build();
            Assert.Equal("XmlValue6", config["CommonKey1:CommonKey2:CommonKey3:CommonKey4"]);

            configurationBuilder.AddInMemoryCollection(_memConfigContent);
            config = configurationBuilder.Build();
            Assert.Equal("MemValue6", config["CommonKey1:CommonKey2:CommonKey3:CommonKey4"]);
        }

        private IConfigurationRoot BuildConfig()
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddIniFile(Path.GetFileName(_iniConfigFilePath));
            configurationBuilder.AddJsonFile(Path.GetFileName(_jsonConfigFilePath));
            configurationBuilder.AddXmlFile(Path.GetFileName(_xmlConfigFilePath));
            return configurationBuilder.Build();
        }

        public class TestIniSourceProvider : IniConfigurationProvider, IConfigurationSource
        {
            public TestIniSourceProvider(string path)
                : base(new IniConfigurationSource { Path = path })
            { }

            public IConfigurationProvider Build(IConfigurationBuilder builder)
            {
                Source.FileProvider = builder.GetFileProvider();
                return this;
            }
        }

        public class TestJsonSourceProvider : JsonConfigurationProvider, IConfigurationSource
        {
            public TestJsonSourceProvider(string path)
                : base(new JsonConfigurationSource { Path = path })
            { }

            public IConfigurationProvider Build(IConfigurationBuilder builder)
            {
                Source.FileProvider = builder.GetFileProvider();
                return this;
            }
        }

        public class TestXmlSourceProvider : XmlConfigurationProvider, IConfigurationSource
        {
            public TestXmlSourceProvider(string path)
                : base(new XmlConfigurationSource { Path = path })
            { }

            public IConfigurationProvider Build(IConfigurationBuilder builder)
            {
                Source.FileProvider = builder.GetFileProvider();
                return this;
            }
        }

        [Fact]
        public void CanSetValuesAndReloadValues()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.Add(new TestIniSourceProvider(Path.GetFileName(_iniConfigFilePath)));
            configurationBuilder.Add(new TestJsonSourceProvider(Path.GetFileName(_jsonConfigFilePath)));
            configurationBuilder.Add(new TestXmlSourceProvider(Path.GetFileName(_xmlConfigFilePath)));

            var config = configurationBuilder.Build();

            // Act & Assert
            // Set value
            config["CommonKey1:CommonKey2:CommonKey3:CommonKey4"] = "NewValue";

            // All config sources must be updated
            foreach (var provider in configurationBuilder.Sources)
            {
                Assert.Equal("NewValue",
                    (provider as FileConfigurationProvider).Get("CommonKey1:CommonKey2:CommonKey3:CommonKey4"));
            }

            // Recover values by reloading
            config.Reload();

            Assert.Equal("XmlValue6", config["CommonKey1:CommonKey2:CommonKey3:CommonKey4"]);

            // Set value with indexer
            config["CommonKey1:CommonKey2:CommonKey3:CommonKey4"] = "NewValue";

            // All config sources must be updated
            foreach (var provider in configurationBuilder.Sources)
            {
                Assert.Equal("NewValue",
                    (provider as FileConfigurationProvider).Get("CommonKey1:CommonKey2:CommonKey3:CommonKey4"));
            }

            // Recover values by reloading
            config.Reload();
            Assert.Equal("XmlValue6", config["CommonKey1:CommonKey2:CommonKey3:CommonKey4"]);
        }

        [Fact]
        public async Task TouchingFileWillReload()
        {
            // Arrange
            File.WriteAllText(Path.Combine(_basePath, "reload.json"), @"{""JsonKey1"": ""JsonValue1""}");
            File.WriteAllText(Path.Combine(_basePath, "reload.ini"), @"IniKey1 = IniValue1");
            File.WriteAllText(Path.Combine(_basePath, "reload.xml"), @"<settings XmlKey1=""XmlValue1""/>");

            var config = new ConfigurationBuilder().AddIniFile(source =>
            {
                source.Path = "reload.ini";
                source.ReloadOnChange = true;
            })
            .AddJsonFile(source =>
            {
                source.Path = "reload.json";
                source.ReloadOnChange = true;
            })
            .AddXmlFile(source =>
            {
                source.Path = "reload.xml";
                source.ReloadOnChange = true;
            }).Build();

            Assert.Equal("JsonValue1", config["JsonKey1"]);
            Assert.Equal("IniValue1", config["IniKey1"]);
            Assert.Equal("XmlValue1", config["XmlKey1"]);

            var token = config.GetReloadToken();

            // Act & Assert
            // Update files
            File.WriteAllText(Path.Combine(_basePath, "reload.json"), @"{""JsonKey1"": ""JsonValue2""}");
            File.WriteAllText(Path.Combine(_basePath, "reload.ini"), @"IniKey1 = IniValue2");
            File.WriteAllText(Path.Combine(_basePath, "reload.xml"), @"<settings XmlKey1=""XmlValue2""/>");

            // NOTE: we'd like to wait for file notification here, but its flaky on CI so we force it
            config.Reload();

            Assert.Equal("JsonValue2", config["JsonKey1"]);
            Assert.Equal("IniValue2", config["IniKey1"]);
            Assert.Equal("XmlValue2", config["XmlKey1"]);
            Assert.True(token.HasChanged);
        }

        [Fact]
        public void LoadIncorrectJsonFile_ThrowFormatException()
        {
            // Arrange
            var json = @"{
                'name': 'test',
                'address': {
                    'street': 'Something street' /*Missing comma*/
                    'zipcode': '12345'
                }
            }";
            var jsonFile = Path.GetRandomFileName();
            File.WriteAllText(jsonFile, json);

            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory());

            // Act & Assert
            var exception = Assert.Throws<FormatException>(() => builder.AddJsonFile(jsonFile).Build());
            Assert.NotNull(exception.Message);

            File.Delete(jsonFile);
        }

        [Fact]
        public void SetBasePathCalledMultipleTimesForEachSourceLastOneWins()
        {

            // Arrange
            var builder = new ConfigurationBuilder();
            var jsonConfigFilePath = "test.json";
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), jsonConfigFilePath), _jsonConfigFileContent);
            var xmlConfigFilePath = "test.xml";
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), xmlConfigFilePath), _xmlConfigFileContent);

            // Act
            builder.AddXmlFile("test.xml")
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("test.json");

            var config = builder.Build();

            // Assert
            Assert.Equal("JsonValue1", config["JsonKey1"]);
            Assert.Equal("JsonValue2", config["Json.Key2:JsonKey3"]);
            Assert.Equal("JsonValue3", config["Json.Key2:Json.Key4"]);
            Assert.Equal("JsonValue4", config["Json.Key2:JsonKey5:JsonKey6"]);
            Assert.Equal("JsonValue5", config["CommonKey1:CommonKey2:JsonKey7"]);

            Assert.Equal("XmlValue1", config["XmlKey1"]);
            Assert.Equal("XmlValue2", config["XmlKey2:XmlKey3"]);
            Assert.Equal("XmlValue3", config["XmlKey2:XmlKey4"]);
            Assert.Equal("XmlValue4", config["XmlKey2:XmlKey5:XmlKey6"]);

            File.Delete(jsonConfigFilePath);
        }

        [Fact]
        public void GetDefaultBasePathForSources()
        {
            // Arrange
            var builder = new ConfigurationBuilder();
            string filePath;

#if NETSTANDARDAPP1_5
            filePath = AppContext.BaseDirectory;
#else
            filePath = Path.GetFullPath(AppDomain.CurrentDomain.GetData("APP_CONTEXT_BASE_DIRECTORY") as string ??
                AppDomain.CurrentDomain.BaseDirectory);
#endif

            var jsonConfigFilePath = Path.Combine(filePath, "test.json");
            var xmlConfigFilePath = Path.Combine(filePath, "xmltest.xml");
            File.WriteAllText(jsonConfigFilePath, _jsonConfigFileContent);
            File.WriteAllText(xmlConfigFilePath, _xmlConfigFileContent);

            // Act
            builder.AddJsonFile("test.json").AddXmlFile("xmltest.xml");

            var config = builder.Build();

            // Assert
            Assert.Equal("JsonValue1", config["JsonKey1"]);
            Assert.Equal("JsonValue2", config["Json.Key2:JsonKey3"]);
            Assert.Equal("JsonValue3", config["Json.Key2:Json.Key4"]);
            Assert.Equal("JsonValue4", config["Json.Key2:JsonKey5:JsonKey6"]);
            Assert.Equal("JsonValue5", config["CommonKey1:CommonKey2:JsonKey7"]);

            Assert.Equal("XmlValue1", config["XmlKey1"]);
            Assert.Equal("XmlValue2", config["XmlKey2:XmlKey3"]);
            Assert.Equal("XmlValue3", config["XmlKey2:XmlKey4"]);
            Assert.Equal("XmlValue4", config["XmlKey2:XmlKey5:XmlKey6"]);

            File.Delete(jsonConfigFilePath);
            File.Delete(xmlConfigFilePath);
        }

        public void Dispose()
        {
            File.Delete(_iniConfigFilePath);
            File.Delete(_xmlConfigFilePath);
            File.Delete(_jsonConfigFilePath);
        }
    }
}
