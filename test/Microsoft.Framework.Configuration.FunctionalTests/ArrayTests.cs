// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.Framework.Configuration.Ini;
using Xunit;

namespace Microsoft.Framework.Configuration.FunctionalTests
{
    public class ArrayTests : IDisposable
    {
        private string _iniConfigFilePath;
        private string _xmlConfigFilePath;
        private string _json1ConfigFilePath;
        private string _json2ConfigFilePath;

        private static readonly string _iniConfigFileContent = @"
[address]
2=ini_2.2.2.2
i=ini_i.i.i.i
";
        private static readonly string _xmlConfigFileContent = @"
<settings>
    <address name=""4"">xml_4.4.4.4</address>
    <address name=""1"">xml_1.1.1.1</address>
    <address name=""x"">xml_x.x.x.x</address>
</settings>   
";
        private static readonly string _json1ConfigFileContent = @"
{
    'address': [
        'json_0.0.0.0',
        'json_1.1.1.1',
        'json_2.2.2.2'
    ]
}
";

        private static readonly string _json2ConfigFileContent = @"
{
    'address': {
        'j': 'json_j.j.j.j',
        '3': 'json_3.3.3.3'
    }
}
";

        [Fact]
        public void DifferentConfigSources_Merged_KeysAreSorted()
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonFile(_json1ConfigFilePath);
            builder.AddIniFile(_iniConfigFilePath);
            builder.AddJsonFile(_json2ConfigFilePath);
            builder.AddXmlFile(_xmlConfigFilePath);

            var config = builder.Build();

            var configurationSection = config.GetSection("address");
            var indexConfigurationSections = configurationSection.GetChildren().ToArray();

            Assert.Equal(8, indexConfigurationSections.Length);
            Assert.Equal("address:0", indexConfigurationSections[0].Key);
            Assert.Equal("address:1", indexConfigurationSections[1].Key);
            Assert.Equal("address:2", indexConfigurationSections[2].Key);
            Assert.Equal("address:3", indexConfigurationSections[3].Key);
            Assert.Equal("address:4", indexConfigurationSections[4].Key);
            Assert.Equal("address:i", indexConfigurationSections[5].Key);
            Assert.Equal("address:j", indexConfigurationSections[6].Key);
            Assert.Equal("address:x", indexConfigurationSections[7].Key);
        }

        [Fact]
        public void DifferentConfigSources_Merged_WithOverwrites()
        {
            var builder = new ConfigurationBuilder();

            builder.AddJsonFile(_json1ConfigFilePath);
            builder.AddIniFile(_iniConfigFilePath);
            builder.AddJsonFile(_json2ConfigFilePath);
            builder.AddXmlFile(_xmlConfigFilePath);

            var config = builder.Build();

            Assert.Equal("json_0.0.0.0", config["address:0"]);
            Assert.Equal("xml_1.1.1.1", config["address:1"]);
            Assert.Equal("ini_2.2.2.2", config["address:2"]);
            Assert.Equal("json_3.3.3.3", config["address:3"]);
            Assert.Equal("xml_4.4.4.4", config["address:4"]);
            Assert.Equal("ini_i.i.i.i", config["address:i"]);
            Assert.Equal("json_j.j.j.j", config["address:j"]);
            Assert.Equal("xml_x.x.x.x", config["address:x"]);
        }

        public ArrayTests()
        {
            _iniConfigFilePath = Path.GetTempFileName();
            _xmlConfigFilePath = Path.GetTempFileName();
            _json1ConfigFilePath = Path.GetTempFileName();
            _json2ConfigFilePath = Path.GetTempFileName();

            File.WriteAllText(_iniConfigFilePath, _iniConfigFileContent);
            File.WriteAllText(_xmlConfigFilePath, _xmlConfigFileContent);
            File.WriteAllText(_json1ConfigFilePath, _json1ConfigFileContent);
            File.WriteAllText(_json2ConfigFilePath, _json2ConfigFileContent);
        }

        public void Dispose()
        {
            File.Delete(_iniConfigFilePath);
            File.Delete(_xmlConfigFilePath);
            File.Delete(_json1ConfigFilePath);
            File.Delete(_json2ConfigFilePath);
        }
    }
}