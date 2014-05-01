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

            string memVal, envVal, iniVal;
            bool memRet, envRet, iniRet;

            // Act
            config.AddLoadedSource(memConfigSrc);
            config.AddLoadedSource(envConfigSrc);
            config.AddLoadedSource(iniConfigSrc);

            memRet = config.TryGet("mem:keyinmem", out memVal);
            envRet = config.TryGet("EnvVariable:KeyInEnv", out envVal);
            iniRet = config.TryGet("INIFILE:KeyInIni", out iniVal);

            // Assert
            Assert.Equal(3, CountAllEntries(config));
            Assert.Contains(memConfigSrc, config);
            Assert.Contains(envConfigSrc, config);
            Assert.Contains(iniConfigSrc, config);

            Assert.True(memRet);
            Assert.True(envRet);
            Assert.True(iniRet);

            Assert.Equal("ValueInMem", memVal);
            Assert.Equal("ValueInEnv", envVal);
            Assert.Equal("ValueInIni", iniVal);

            Assert.Equal("ValueInMem", config.Get("mem:keyinmem"));
            Assert.Equal("ValueInEnv", config.Get("EnvVariable:KEYINENV"));
            Assert.Equal("ValueInIni", config.Get("IniFile:KeyInIni"));
            Assert.Null(config.Get("NotExist"));
        }

        [Fact]
        public void NewConfigurationSourceOverridesOldOneWhenKeyIsDuplicated()
        {
            // Arrange
            var dic = new Dictionary<string, string>()
                { 
                    {"Key1:Key2", "ValueInMem"}
                };
            var memConfigSrc = new MemoryConfigurationSource(dic);

            var ini = @"
            [Key1]
            Key2=ValueInIni";
            var iniConfigSrc = new IniFileConfigurationSource(ArbitraryFilePath);
            iniConfigSrc.Load(StringToStream(ini));

            var config = new Configuration();

            // Act
            config.AddLoadedSource(memConfigSrc);
            config.AddLoadedSource(iniConfigSrc);

            // Assert
            Assert.Equal(2, CountAllEntries(config));
            Assert.Equal("ValueInIni", config.Get("Key1:Key2"));
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
            config.AddLoadedSource(iniConfigSrc);
            config.AddLoadedSource(envConfigSrc);

            // Act
            config.Set("Key", "NewValue");

            // Assert
            Assert.Equal(3, CountAllEntries(config));
            Assert.Equal("NewValue", config.Get("Key"));
            Assert.Equal("NewValue", memConfigSrc.Data["Key"]);
            Assert.Equal("NewValue", envConfigSrc.Data["Key"]);
            Assert.Equal("NewValue", iniConfigSrc.Data["Key"]);
        }

        [Fact]
        public void CanGetSubKey()
        {
            // Arrange
            var dic = new Dictionary<string, string>()
                { 
                    {"Data:DB1:Connection1", "MemVal1"},
                    {"Data:DB1:Connection2", "MemVal2"}
                };
            var memConfigSrc = new MemoryConfigurationSource(dic);

            var ini = @"
            DataSource:DB2:Connection=IniVal";
            var iniConfigSrc = new IniFileConfigurationSource(ArbitraryFilePath);
            iniConfigSrc.Load(StringToStream(ini));

            var hashTable = new Hashtable()
                {
                    {"Data", "EnvVal"}
                };
            var envConfigSrc = new EnvironmentVariablesConfigurationSource();
            envConfigSrc.Load(hashTable);

            var config = new Configuration();
            config.AddLoadedSource(memConfigSrc);
            config.AddLoadedSource(iniConfigSrc);
            config.AddLoadedSource(envConfigSrc);

            string memVal1, memVal2, iniVal1, iniVal2, envVal1;
            bool memRet1, memRet2, iniRet1, iniRet2, envRet1;

            // Act
            var configFocus = config.GetSubKey("Data");

            memRet1 = configFocus.TryGet("DB1:Connection1", out memVal1);
            memRet2 = configFocus.TryGet("DB1:Connection2", out memVal2);
            iniRet1 = configFocus.TryGet("DB2:Connection", out iniVal1);
            iniRet2 = configFocus.TryGet("Source:DB2:Connection", out iniVal2);
            envRet1 = configFocus.TryGet(null, out envVal1);

            // Assert
            Assert.True(memRet1);
            Assert.True(memRet2);
            Assert.False(iniRet1);
            Assert.False(iniRet2);
            Assert.True(envRet1);

            Assert.Equal("MemVal1", memVal1);
            Assert.Equal("MemVal2", memVal2);
            Assert.Equal("EnvVal", envVal1);

            Assert.Equal("MemVal1", configFocus.Get("DB1:Connection1"));
            Assert.Equal("MemVal2", configFocus.Get("DB1:Connection2"));
            Assert.Null(configFocus.Get("DB2:Connection"));
            Assert.Null(configFocus.Get("Source:DB2:Connection"));
            Assert.Equal("EnvVal", configFocus.Get(null));
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

            var ini = @"
            Data:DB2Connection=IniValue";
            var iniConfigSrc = new IniFileConfigurationSource(ArbitraryFilePath);
            iniConfigSrc.Load(StringToStream(ini));

            var hashTable = new Hashtable()
                {
                    {"DataSource:DB3:Connection", "EnvValue"}
                };
            var envConfigSrc = new EnvironmentVariablesConfigurationSource();
            envConfigSrc.Load(hashTable);

            var config = new Configuration();
            config.AddLoadedSource(memConfigSrc);
            config.AddLoadedSource(iniConfigSrc);
            config.AddLoadedSource(envConfigSrc);

            // Act
            var configFocusList = config.GetSubKeys("Data");
            var subKeysSet = configFocusList.ToDictionary(e => e.Key, e => e.Value);

            // Assert
            Assert.Equal(2, configFocusList.Count());
            Assert.Equal("MemValue1", subKeysSet["DB1"].Get("Connection1"));
            Assert.Equal("MemValue2", subKeysSet["DB1"].Get("Connection2"));
            Assert.Equal("IniValue", subKeysSet["DB2Connection"].Get(null));
            Assert.False(subKeysSet.ContainsKey("DB3"));
            Assert.False(subKeysSet.ContainsKey("Source:DB3"));
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
                    envConfigSrc,
                    iniConfigSrc
                };

            var config = new Configuration();

            // Act
            config.AddLoadedSource(memConfigSrc);
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

            Assert.Equal(3, srcCount);
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
                    envConfigSrc,
                    iniConfigSrc
                };

            var config = new Configuration();

            // Act
            config.AddLoadedSource(memConfigSrc);
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

            Assert.Equal(3, srcCount);
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
