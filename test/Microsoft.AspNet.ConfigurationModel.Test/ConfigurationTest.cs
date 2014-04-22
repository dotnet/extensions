using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNet.ConfigurationModel.Sources;
using Xunit;

namespace Microsoft.AspNet.ConfigurationModel
{
    public class ConfigurationTest
    {
        private static readonly string ArbitraryFilePath = "Unit tests do not touch file system";

        [Fact]
        public void LoadAndCombineKeyValuePairsFromDifferentConfigurationSources()
        {
            // Arrange
            var dic = new Dictionary<string, string>()
                { 
                    {"Mem:KeyInMem", "ValueInMem"}
                };
            var memConfigSrc = new MemoryConfigurationSource(dic);

            var json = @"{
                'JsonFile': {
                    'KeyInJson': 'ValueInJson'
                }
            }";
            var jsonConfigSrc = new JsonConfigurationSource(ArbitraryFilePath);
            jsonConfigSrc.Load(StringToStream(json));

            var xml = 
                @"<settings>
                    <XmlFile>
                        <KeyInXml>ValueInXml</KeyInXml>
                    </XmlFile>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationSource(ArbitraryFilePath);
            xmlConfigSrc.Load(StringToStream(xml));

            var hashTable = new Hashtable()
                {
                    {"EnvVariable:KeyInEnv", "ValueInEnv"}
                };
            var envConfigSrc = new EnvironmentVariablesConfigurationSource();
            envConfigSrc.Load(hashTable);

            var ini = @"
            [IniFile]
            KeyInIni=ValueInIni ";
            var iniConfigSrc = new IniFileConfigurationSource(ArbitraryFilePath);
            iniConfigSrc.Load(StringToStream(ini));

            var config = new Configuration();

            string memVal, jsonVal, xmlVal, envVal, iniVal;
            bool memRet, jsonRet, xmlRet, envRet, iniRet;

            // Act
            config.AddLoadedSource(memConfigSrc);
            config.AddLoadedSource(jsonConfigSrc);
            config.AddLoadedSource(xmlConfigSrc);
            config.AddLoadedSource(envConfigSrc);
            config.AddLoadedSource(iniConfigSrc);

            memRet = config.TryGet("Mem:KeyInMem", out memVal);
            jsonRet = config.TryGet("JsonFile:KeyInJson", out jsonVal);
            xmlRet = config.TryGet("XmlFile:KeyInXml", out xmlVal);
            envRet = config.TryGet("EnvVariable:KeyInEnv", out envVal);
            iniRet = config.TryGet("IniFile:KeyInIni", out iniVal);

            // Assert
            Assert.Equal(5, CountAllEntries(config));
            Assert.Contains(memConfigSrc, config);
            Assert.Contains(jsonConfigSrc, config);
            Assert.Contains(xmlConfigSrc, config);
            Assert.Contains(envConfigSrc, config);
            Assert.Contains(iniConfigSrc, config);

            Assert.True(memRet);
            Assert.True(jsonRet);
            Assert.True(xmlRet);
            Assert.True(envRet);
            Assert.True(iniRet);

            Assert.Equal("ValueInMem", memVal);
            Assert.Equal("ValueInJson", jsonVal);
            Assert.Equal("ValueInXml", xmlVal);
            Assert.Equal("ValueInEnv", envVal);
            Assert.Equal("ValueInIni", iniVal);

            Assert.Equal("ValueInMem", config.Get("Mem:KeyInMem"));
            Assert.Equal("ValueInJson", config.Get("JsonFile:KeyInJson"));
            Assert.Equal("ValueInXml", config.Get("XmlFile:KeyInXml"));
            Assert.Equal("ValueInEnv", config.Get("EnvVariable:KeyInEnv"));
            Assert.Equal("ValueInIni", config.Get("IniFile:KeyInIni"));
            Assert.Null(config.Get("NotExist"));
        }

        [Fact]
        public void NewConfigurationSourceOverridesOldOneWhenKeyIsDuplicated()
        {
            // Arrange
            var json = @"{
                'Key1': {
                    'Key2': 'ValueInJson'
                }
            }";
            var jsonConfigSrc = new JsonConfigurationSource(ArbitraryFilePath);
            jsonConfigSrc.Load(StringToStream(json));

            var xml = 
                @"<settings>
                    <Key1>
                        <Key2>ValueInXml</Key2>
                    </Key1>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationSource(ArbitraryFilePath);
            xmlConfigSrc.Load(StringToStream(xml));

            var config = new Configuration();

            // Act
            config.AddLoadedSource(jsonConfigSrc);
            config.AddLoadedSource(xmlConfigSrc);

            // Assert
            Assert.Equal(2, CountAllEntries(config));
            Assert.Equal("ValueInXml", config.Get("Key1:Key2"));
        }

        [Fact]
        public void SettingValueUpdatesAllConfigurationSources()
        {
            // Arrange
            var dic = new Dictionary<string, string>()
                { 
                    {"Key", "Value"}
                };
            var memConfigSrc = new MemoryConfigurationSource(dic);

            var json = @"{
                'Key': 'Value'
                }";
            var jsonConfigSrc = new JsonConfigurationSource(ArbitraryFilePath);
            jsonConfigSrc.Load(StringToStream(json));

            var xml = 
                @"<settings>
                    <Key>Value</Key>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationSource(ArbitraryFilePath);
            xmlConfigSrc.Load(StringToStream(xml));

            var hashTable = new Hashtable()
                {
                    {"Key", "Value"}
                };
            var envConfigSrc = new EnvironmentVariablesConfigurationSource();
            envConfigSrc.Load(hashTable);

            var ini = @"
            Key=Value";
            var iniConfigSrc = new IniFileConfigurationSource(ArbitraryFilePath);
            iniConfigSrc.Load(StringToStream(ini));

            var config = new Configuration();
            config.AddLoadedSource(memConfigSrc);
            config.AddLoadedSource(jsonConfigSrc);
            config.AddLoadedSource(xmlConfigSrc);
            config.AddLoadedSource(iniConfigSrc);
            config.AddLoadedSource(envConfigSrc);

            // Act
            config.Set("Key", "NewValue");

            // Assert
            Assert.Equal(5, CountAllEntries(config));
            Assert.Equal("NewValue", config.Get("Key"));
            Assert.Equal("NewValue", memConfigSrc.Data["Key"]);
            Assert.Equal("NewValue", jsonConfigSrc.Data["Key"]);
            Assert.Equal("NewValue", xmlConfigSrc.Data["Key"]);
            Assert.Equal("NewValue", envConfigSrc.Data["Key"]);
            Assert.Equal("NewValue", iniConfigSrc.Data["Key"]);
        }

        [Fact]
        public void CanGetSubKey()
        {
            // Arrange
            var json = @"{
                            'Data': {
                                'DB1': {
                                    'Connection1': 'JsonValue1',
                                    'Connection2': 'JsonValue2'
                                }
                            }
                        }";
            var jsonConfigSrc = new JsonConfigurationSource(ArbitraryFilePath);
            jsonConfigSrc.Load(StringToStream(json));

            var xml = 
                @"<settings>
                    <Data Name='DB2' Connection1='XmlValue1' />
                    <Data Name='DB2' Connection2='XmlValue2' />
                </settings>";
            var xmlConfigSrc = new XmlConfigurationSource(ArbitraryFilePath);
            xmlConfigSrc.Load(StringToStream(xml));

            var ini = @"
            DataSource:DB3:Connection=IniValue";
            var iniConfigSrc = new IniFileConfigurationSource(ArbitraryFilePath);
            iniConfigSrc.Load(StringToStream(ini));

            var hashTable = new Hashtable()
                {
                    {"Data", "EnvValue"}
                };
            var envConfigSrc = new EnvironmentVariablesConfigurationSource();
            envConfigSrc.Load(hashTable);

            var config = new Configuration();
            config.AddLoadedSource(jsonConfigSrc);
            config.AddLoadedSource(xmlConfigSrc);
            config.AddLoadedSource(iniConfigSrc);
            config.AddLoadedSource(envConfigSrc);

            string jsonVal1, jsonVal2, xmlVal1, xmlVal2, iniVal1, iniVal2, envVal;
            bool jsonRet1, jsonRet2, xmlRet1, xmlRet2, iniRet1, iniRet2, envRet;

            // Act
            var configFocus = config.GetSubKey("Data");

            jsonRet1 = configFocus.TryGet("DB1:Connection1", out jsonVal1);
            jsonRet2 = configFocus.TryGet("DB1:Connection2", out jsonVal2);
            xmlRet1 = configFocus.TryGet("DB2:Connection1", out xmlVal1);
            xmlRet2 = configFocus.TryGet("DB2:Connection2", out xmlVal2);
            iniRet1 = configFocus.TryGet("DB3:Connection", out iniVal1);
            iniRet2 = configFocus.TryGet("Source:DB3:Connection", out iniVal2);
            envRet = configFocus.TryGet(null, out envVal);

            // Assert
            Assert.True(jsonRet1);
            Assert.True(jsonRet2);
            Assert.True(xmlRet1);
            Assert.True(xmlRet2);
            Assert.False(iniRet1);
            Assert.False(iniRet2);
            Assert.True(envRet);

            Assert.Equal("JsonValue1", jsonVal1);
            Assert.Equal("JsonValue2", jsonVal2);
            Assert.Equal("XmlValue1", xmlVal1);
            Assert.Equal("XmlValue2", xmlVal2);
            Assert.Equal("EnvValue", envVal);

            Assert.Equal("JsonValue1", configFocus.Get("DB1:Connection1"));
            Assert.Equal("JsonValue2", configFocus.Get("DB1:Connection2"));
            Assert.Equal("XmlValue1", configFocus.Get("DB2:Connection1"));
            Assert.Equal("XmlValue2", configFocus.Get("DB2:Connection2"));
            Assert.Null(configFocus.Get("DB3:Connection"));
            Assert.Null(configFocus.Get("Source:DB3:Connection"));
            Assert.Equal("EnvValue", configFocus.Get(null));
        }

        [Fact]
        public void CanGetSubKeys()
        {
            // Arrange
            var dic = new Dictionary<string, string>()
                { 
                    {"Data:DB1:Connection1", "MemValue1"},
                    {"Data:DB1:Connection2", "MemValue2"}
                };
            var memConfigSrc = new MemoryConfigurationSource(dic);

            var json = @"{
                            'Data': {
                                'DB2': {
                                    'Connection1': 'JsonValue1',
                                    'Connection2': 'JsonValue2'
                                }
                            }
                        }";
            var jsonConfigSrc = new JsonConfigurationSource(ArbitraryFilePath);
            jsonConfigSrc.Load(StringToStream(json));

            var xml = 
                @"<settings>
                    <Data DB3Connection1='XmlValue1' />
                    <Data DB3Connection2='XmlValue2' />
                </settings>";
            var xmlConfigSrc = new XmlConfigurationSource(ArbitraryFilePath);
            xmlConfigSrc.Load(StringToStream(xml));

            var ini = @"
            DataSource:DB4:Connection1=IniValue1
            DataSource:DB4:Connection2=IniValue2";
            var iniConfigSrc = new IniFileConfigurationSource(ArbitraryFilePath);
            iniConfigSrc.Load(StringToStream(ini));

            var config = new Configuration();
            config.AddLoadedSource(memConfigSrc);
            config.AddLoadedSource(jsonConfigSrc);
            config.AddLoadedSource(xmlConfigSrc);
            config.AddLoadedSource(iniConfigSrc);

            // Act
            var configFocusList = config.GetSubKeys("Data");
            var subKeysSet = configFocusList.ToDictionary(e => e.Key, e => e.Value);

            // Assert
            Assert.Equal(4, configFocusList.Count());
            Assert.Equal("MemValue1", subKeysSet["DB1"].Get("Connection1"));
            Assert.Equal("MemValue2", subKeysSet["DB1"].Get("Connection2"));
            Assert.Equal("JsonValue1", subKeysSet["DB2"].Get("Connection1"));
            Assert.Equal("JsonValue2", subKeysSet["DB2"].Get("Connection2"));
            Assert.Equal("XmlValue1", subKeysSet["DB3Connection1"].Get(null));
            Assert.Equal("XmlValue2", subKeysSet["DB3Connection2"].Get(null));
            Assert.False(subKeysSet.ContainsKey("DB4"));
            Assert.False(subKeysSet.ContainsKey("Source:DB4"));
        }

        [Fact]
        public void CanIterateWithGenericEnumerator()
        {
            // Arrange
            var dic = new Dictionary<string, string>()
                { 
                    {"Mem:KeyInMem", "ValueInMem"}
                };
            var memConfigSrc = new MemoryConfigurationSource(dic);

            var json = @"{
                'JsonFile': {
                    'KeyInJson': 'ValueInJson'
                }
            }";
            var jsonConfigSrc = new JsonConfigurationSource(ArbitraryFilePath);
            jsonConfigSrc.Load(StringToStream(json));

            var xml = 
                @"<settings>
                    <XmlFile>
                        <KeyInXml>ValueInXml</KeyInXml>
                    </XmlFile>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationSource(ArbitraryFilePath);
            xmlConfigSrc.Load(StringToStream(xml));

            var hashTable = new Hashtable()
                {
                    {"EnvVariable:KeyInEnv", "ValueInEnv"}
                };
            var envConfigSrc = new EnvironmentVariablesConfigurationSource();
            envConfigSrc.Load(hashTable);

            var ini = @"
            [IniFile]
            KeyInIni=ValueInIni ";
            var iniConfigSrc = new IniFileConfigurationSource(ArbitraryFilePath);
            iniConfigSrc.Load(StringToStream(ini));

            var srcSet = new HashSet<IConfigurationSource>()
                {
                    memConfigSrc,
                    jsonConfigSrc,
                    xmlConfigSrc,
                    envConfigSrc,
                    iniConfigSrc
                };

            var config = new Configuration();

            // Act
            config.AddLoadedSource(memConfigSrc);
            config.AddLoadedSource(jsonConfigSrc);
            config.AddLoadedSource(xmlConfigSrc);
            config.AddLoadedSource(envConfigSrc);
            config.AddLoadedSource(iniConfigSrc);


            // Assert
            var enumerator = config.GetEnumerator();
            int srcCount = 0;
            while (enumerator.MoveNext())
            {
                Assert.Contains(enumerator.Current, srcSet);
                ++srcCount;
            }

            Assert.Equal(5, srcCount);
        }

        [Fact]
        public void CanIterateAfterCastedToIEnumerable()
        {
            // Arrange
            var dic = new Dictionary<string, string>()
                { 
                    {"Mem:KeyInMem", "ValueInMem"}
                };
            var memConfigSrc = new MemoryConfigurationSource(dic);

            var json = @"{
                'JsonFile': {
                    'KeyInJson': 'ValueInJson'
                }
            }";
            var jsonConfigSrc = new JsonConfigurationSource(ArbitraryFilePath);
            jsonConfigSrc.Load(StringToStream(json));

            var xml = 
                @"<settings>
                    <XmlFile>
                        <KeyInXml>ValueInXml</KeyInXml>
                    </XmlFile>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationSource(ArbitraryFilePath);
            xmlConfigSrc.Load(StringToStream(xml));

            var hashTable = new Hashtable()
                {
                    {"EnvVariable:KeyInEnv", "ValueInEnv"}
                };
            var envConfigSrc = new EnvironmentVariablesConfigurationSource();
            envConfigSrc.Load(hashTable);

            var ini = @"
            [IniFile]
            KeyInIni=ValueInIni ";
            var iniConfigSrc = new IniFileConfigurationSource(ArbitraryFilePath);
            iniConfigSrc.Load(StringToStream(ini));

            var srcSet = new HashSet<IConfigurationSource>()
                {
                    memConfigSrc,
                    jsonConfigSrc,
                    xmlConfigSrc,
                    envConfigSrc,
                    iniConfigSrc
                };

            var config = new Configuration();

            // Act
            config.AddLoadedSource(memConfigSrc);
            config.AddLoadedSource(jsonConfigSrc);
            config.AddLoadedSource(xmlConfigSrc);
            config.AddLoadedSource(envConfigSrc);
            config.AddLoadedSource(iniConfigSrc);

            var enumerable = config as IEnumerable;

            // Assert
            var enumerator = config.GetEnumerator();
            int srcCount = 0;
            while (enumerator.MoveNext())
            {
                Assert.Contains(enumerator.Current, srcSet);
                ++srcCount;
            }

            Assert.Equal(5, srcCount);
        }

        private static int CountAllEntries(Configuration config)
        {
            return config.Aggregate(0, (acc, src) => acc + (src as BaseConfigurationSource).Data.Count);
        }

        private static Stream StringToStream(string str)
        {
            var memStream = new MemoryStream();
            var textWriter = new StreamWriter(memStream);
            textWriter.Write(str);
            textWriter.Flush();
            memStream.Seek(0, SeekOrigin.Begin);

            return memStream;
        }
    }
}
