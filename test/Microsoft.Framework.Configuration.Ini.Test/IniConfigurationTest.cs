// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Framework.Configuration.Test;
using Xunit;

namespace Microsoft.Framework.Configuration.Ini.Test
{
    public class IniConfigurationTest
    {
        [Fact]
        public void LoadKeyValuePairsFromValidIniFile()
        {
            var ini = @"[DefaultConnection]
ConnectionString=TestConnectionString
Provider=SqlClient
[Data:Inventory]
ConnectionString=AnotherTestConnectionString
SubHeader:Provider=MySql";
            var iniConfigSrc = new IniConfigurationProvider(TestStreamHelpers.ArbitraryFilePath);

            iniConfigSrc.Load(TestStreamHelpers.StringToStream(ini));

            Assert.Equal("TestConnectionString", iniConfigSrc.Get("defaultconnection:ConnectionString"));
            Assert.Equal("SqlClient", iniConfigSrc.Get("DEFAULTCONNECTION:PROVIDER"));
            Assert.Equal("AnotherTestConnectionString", iniConfigSrc.Get("Data:Inventory:CONNECTIONSTRING"));
            Assert.Equal("MySql", iniConfigSrc.Get("Data:Inventory:SubHeader:Provider"));
        }

        [Fact]
        public void LoadMethodCanHandleEmptyValue()
        {
            var ini = @"DefaultKey=";
            var iniConfigSrc = new IniConfigurationProvider(TestStreamHelpers.ArbitraryFilePath);

            iniConfigSrc.Load(TestStreamHelpers.StringToStream(ini));

            Assert.Equal(string.Empty, iniConfigSrc.Get("DefaultKey"));
        }

        [Fact]
        public void LoadKeyValuePairsFromValidIniFileWithQuotedValues()
        {
            var ini = "[DefaultConnection]\n" +
                      "ConnectionString=\"TestConnectionString\"\n" +
                      "Provider=\"SqlClient\"\n" +
                      "[Data:Inventory]\n" +
                      "ConnectionString=\"AnotherTestConnectionString\"\n" +
                      "Provider=\"MySql\"";
            var iniConfigSrc = new IniConfigurationProvider(TestStreamHelpers.ArbitraryFilePath);

            iniConfigSrc.Load(TestStreamHelpers.StringToStream(ini));

            Assert.Equal("TestConnectionString", iniConfigSrc.Get("DefaultConnection:ConnectionString"));
            Assert.Equal("SqlClient", iniConfigSrc.Get("DefaultConnection:Provider"));
            Assert.Equal("AnotherTestConnectionString", iniConfigSrc.Get("Data:Inventory:ConnectionString"));
            Assert.Equal("MySql", iniConfigSrc.Get("Data:Inventory:Provider"));
        }

        [Fact]
        public void DoubleQuoteIsPartOfValueIfNotPaired()
        {
            var ini = "[ConnectionString]\n" +
                      "DefaultConnection=\"TestConnectionString\n" +
                      "Provider=SqlClient\"";
            var iniConfigSrc = new IniConfigurationProvider(TestStreamHelpers.ArbitraryFilePath);

            iniConfigSrc.Load(TestStreamHelpers.StringToStream(ini));

            Assert.Equal("\"TestConnectionString", iniConfigSrc.Get("ConnectionString:DefaultConnection"));
            Assert.Equal("SqlClient\"", iniConfigSrc.Get("ConnectionString:Provider"));
        }

        [Fact]
        public void DoubleQuoteIsPartOfValueIfAppearInTheMiddleOfValue()
        {
            var ini = "[ConnectionString]\n" +
                      "DefaultConnection=Test\"Connection\"String\n" +
                      "Provider=Sql\"Client";
            var iniConfigSrc = new IniConfigurationProvider(TestStreamHelpers.ArbitraryFilePath);

            iniConfigSrc.Load(TestStreamHelpers.StringToStream(ini));

            Assert.Equal("Test\"Connection\"String", iniConfigSrc.Get("ConnectionString:DefaultConnection"));
            Assert.Equal("Sql\"Client", iniConfigSrc.Get("ConnectionString:Provider"));
        }

        [Fact]
        public void LoadKeyValuePairsFromValidIniFileWithoutSectionHeader()
        {
            var ini = @"
            DefaultConnection:ConnectionString=TestConnectionString
            DefaultConnection:Provider=SqlClient
            Data:Inventory:ConnectionString=AnotherTestConnectionString
            Data:Inventory:Provider=MySql
            ";
            var iniConfigSrc = new IniConfigurationProvider(TestStreamHelpers.ArbitraryFilePath);

            iniConfigSrc.Load(TestStreamHelpers.StringToStream(ini));

            Assert.Equal("TestConnectionString", iniConfigSrc.Get("DefaultConnection:ConnectionString"));
            Assert.Equal("SqlClient", iniConfigSrc.Get("DefaultConnection:Provider"));
            Assert.Equal("AnotherTestConnectionString", iniConfigSrc.Get("Data:Inventory:ConnectionString"));
            Assert.Equal("MySql", iniConfigSrc.Get("Data:Inventory:Provider"));
        }

        [Fact]
        public void SupportAndIgnoreComments()
        {
            var ini = @"
            ; Comments
            [DefaultConnection]
            # Comments
            ConnectionString=TestConnectionString
            / Comments
            Provider=SqlClient
            [Data:Inventory]
            ConnectionString=AnotherTestConnectionString
            Provider=MySql
            ";
            var iniConfigSrc = new IniConfigurationProvider(TestStreamHelpers.ArbitraryFilePath);

            iniConfigSrc.Load(TestStreamHelpers.StringToStream(ini));

            Assert.Equal("TestConnectionString", iniConfigSrc.Get("DefaultConnection:ConnectionString"));
            Assert.Equal("SqlClient", iniConfigSrc.Get("DefaultConnection:Provider"));
            Assert.Equal("AnotherTestConnectionString", iniConfigSrc.Get("Data:Inventory:ConnectionString"));
            Assert.Equal("MySql", iniConfigSrc.Get("Data:Inventory:Provider"));
        }

        [Fact]
        public void ThrowExceptionWhenFoundInvalidLine()
        {
            var ini = @"
ConnectionString
            ";
            var iniConfigSrc = new IniConfigurationProvider(TestStreamHelpers.ArbitraryFilePath);
            var expectedMsg = Resources.FormatError_UnrecognizedLineFormat("ConnectionString");

            var exception = Assert.Throws<FormatException>(() => iniConfigSrc.Load(TestStreamHelpers.StringToStream(ini)));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenFoundBrokenSectionHeader()
        {
            var ini = @"
[ConnectionString
DefaultConnection=TestConnectionString
            ";
            var iniConfigSrc = new IniConfigurationProvider(TestStreamHelpers.ArbitraryFilePath);
            var expectedMsg = Resources.FormatError_UnrecognizedLineFormat("[ConnectionString");

            var exception = Assert.Throws<FormatException>(() => iniConfigSrc.Load(TestStreamHelpers.StringToStream(ini)));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenPassingNullAsFilePath()
        {
            var expectedMsg = new ArgumentException(Resources.Error_InvalidFilePath, "path").Message;

            var exception = Assert.Throws<ArgumentException>(() => new IniConfigurationProvider(null));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenPassingEmptyStringAsFilePath()
        {
            var expectedMsg = new ArgumentException(Resources.Error_InvalidFilePath, "path").Message;

            var exception = Assert.Throws<ArgumentException>(() => new IniConfigurationProvider(string.Empty));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenKeyIsDuplicated()
        {
            var ini = @"
            [Data:DefaultConnection]
            ConnectionString=TestConnectionString
            Provider=SqlClient
            [Data]
            DefaultConnection:ConnectionString=AnotherTestConnectionString
            Provider=MySql
            ";
            var iniConfigSrc = new IniConfigurationProvider(TestStreamHelpers.ArbitraryFilePath);
            var expectedMsg = Resources.FormatError_KeyIsDuplicated("Data:DefaultConnection:ConnectionString");

            var exception = Assert.Throws<FormatException>(() => iniConfigSrc.Load(TestStreamHelpers.StringToStream(ini)));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void IniConfiguration_Throws_On_Missing_Configuration_File()
        {
            var configSource = new IniConfigurationProvider("NotExistingConfig.ini", optional: false);
            Assert.Throws<FileNotFoundException>(() =>
            {
                try
                {
                    configSource.Load();
                }
                catch (FileNotFoundException exception)
                {
                    Assert.Equal(Resources.FormatError_FileNotFound("NotExistingConfig.ini"), exception.Message);
                    throw;
                }
            });
        }

        [Fact]
        public void IniConfiguration_Does_Not_Throw_On_Optional_Configuration()
        {
            var configSource = new IniConfigurationProvider("NotExistingConfig.ini", optional: true);
            configSource.Load();
            Assert.Throws<InvalidOperationException>(() => configSource.Get("key"));
        }
    }
}