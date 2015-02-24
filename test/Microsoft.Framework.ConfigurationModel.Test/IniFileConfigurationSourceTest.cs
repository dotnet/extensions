// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Xunit;

namespace Microsoft.Framework.ConfigurationModel
{
    public class IniFileConfigurationSourceTest
    {
        private static readonly string ArbitraryFilePath = "Unit tests do not touch file system";

        [Fact]
        public void LoadKeyValuePairsFromValidIniFile()
        {
            var ini = @"[DefaultConnection]
ConnectionString=TestConnectionString
Provider=SqlClient
[Data:Inventory]
ConnectionString=AnotherTestConnectionString
SubHeader:Provider=MySql";
            var iniConfigSrc = new IniFileConfigurationSource(ArbitraryFilePath);

            iniConfigSrc.Load(StringToStream(ini));

            Assert.Equal("TestConnectionString", iniConfigSrc.Get("defaultconnection:ConnectionString"));
            Assert.Equal("SqlClient", iniConfigSrc.Get("DEFAULTCONNECTION:PROVIDER"));
            Assert.Equal("AnotherTestConnectionString", iniConfigSrc.Get("Data:Inventory:CONNECTIONSTRING"));
            Assert.Equal("MySql", iniConfigSrc.Get("Data:Inventory:SubHeader:Provider"));
        }

        [Fact]
        public void LoadMethodCanHandleEmptyValue()
        {
            var ini = @"DefaultKey=";
            var iniConfigSrc = new IniFileConfigurationSource(ArbitraryFilePath);

            iniConfigSrc.Load(StringToStream(ini));

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
            var iniConfigSrc = new IniFileConfigurationSource(ArbitraryFilePath);

            iniConfigSrc.Load(StringToStream(ini));

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
            var iniConfigSrc = new IniFileConfigurationSource(ArbitraryFilePath);

            iniConfigSrc.Load(StringToStream(ini));

            Assert.Equal("\"TestConnectionString", iniConfigSrc.Get("ConnectionString:DefaultConnection"));
            Assert.Equal("SqlClient\"", iniConfigSrc.Get("ConnectionString:Provider"));
        }

        [Fact]
        public void DoubleQuoteIsPartOfValueIfAppearInTheMiddleOfValue()
        {
            var ini = "[ConnectionString]\n" +
                      "DefaultConnection=Test\"Connection\"String\n" +
                      "Provider=Sql\"Client";
            var iniConfigSrc = new IniFileConfigurationSource(ArbitraryFilePath);

            iniConfigSrc.Load(StringToStream(ini));

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
            var iniConfigSrc = new IniFileConfigurationSource(ArbitraryFilePath);

            iniConfigSrc.Load(StringToStream(ini));

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
            var iniConfigSrc = new IniFileConfigurationSource(ArbitraryFilePath);

            iniConfigSrc.Load(StringToStream(ini));

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
            var iniConfigSrc = new IniFileConfigurationSource(ArbitraryFilePath);
            var expectedMsg = Resources.FormatError_UnrecognizedLineFormat("ConnectionString");

            var exception = Assert.Throws<FormatException>(() => iniConfigSrc.Load(StringToStream(ini)));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenFoundBrokenSectionHeader()
        {
            var ini = @"
[ConnectionString
DefaultConnection=TestConnectionString
            ";
            var iniConfigSrc = new IniFileConfigurationSource(ArbitraryFilePath);
            var expectedMsg = Resources.FormatError_UnrecognizedLineFormat("[ConnectionString");
            
            var exception = Assert.Throws<FormatException>(() => iniConfigSrc.Load(StringToStream(ini)));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenPassingNullAsFilePath()
        {
            var expectedMsg = new ArgumentException(Resources.Error_InvalidFilePath, "path").Message;

            var exception = Assert.Throws<ArgumentException>(() => new IniFileConfigurationSource(null));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenPassingEmptyStringAsFilePath()
        {
            var expectedMsg = new ArgumentException(Resources.Error_InvalidFilePath, "path").Message;

            var exception = Assert.Throws<ArgumentException>(() => new IniFileConfigurationSource(string.Empty));

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
            var iniConfigSrc = new IniFileConfigurationSource(ArbitraryFilePath);
            var expectedMsg = Resources.FormatError_KeyIsDuplicated("Data:DefaultConnection:ConnectionString");

            var exception = Assert.Throws<FormatException>(() => iniConfigSrc.Load(StringToStream(ini)));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void CommitMethodPreservesCommentsAndWhiteSpaces()
        {
            var ini = @"
            ; Comments
            [Data:DefaultConnection]
            # Comments
            ConnectionString=TestConnectionString
            / Comments
            Provider=SqlClient";
            var iniConfigSrc = new IniFileConfigurationSource(ArbitraryFilePath);
            var outputCacheStream = new MemoryStream();
            iniConfigSrc.Load(StringToStream(ini));

            iniConfigSrc.Commit(StringToStream(ini), outputCacheStream);

            var newContents = StreamToString(outputCacheStream);
            Assert.Equal(ini, newContents);
        }

        [Fact]
        public void CommitMethodUpdatesValues()
        {
            var ini = @"; Comments
[Data:DefaultConnection]
# Comments
ConnectionString=TestConnectionString
/ Comments
Provider=SqlClient";
            var iniConfigSrc = new IniFileConfigurationSource(ArbitraryFilePath);
            var outputCacheStream = new MemoryStream();
            iniConfigSrc.Load(StringToStream(ini));
            iniConfigSrc.Set("Data:DefaultConnection:ConnectionString", "NewTestConnectionString");

            iniConfigSrc.Commit(StringToStream(ini), outputCacheStream);

            var newContents = StreamToString(outputCacheStream);
            Assert.Equal(ini.Replace("TestConnectionString", "NewTestConnectionString"), newContents);
        }

        [Fact]
        public void CommitMethodCanHandleEmptyValue()
        {
            var ini = @"DefaultKey=";
            var iniConfigSrc = new IniFileConfigurationSource(ArbitraryFilePath);
            var outputCacheStream = new MemoryStream();
            iniConfigSrc.Load(StringToStream(ini));
            iniConfigSrc.Set("DefaultKey", "Value");

            iniConfigSrc.Commit(StringToStream(ini), outputCacheStream);

            var newContents = StreamToString(outputCacheStream);
            Assert.Equal("DefaultKey=Value", newContents);
        }

        [Fact]
        public void CommitOperationThrowsExceptionWhenFindInvalidModificationAfterLoadOperation()
        {
            var ini = @"
            ; Comments
            [Data:DefaultConnection]
            # Comments
            ConnectionString=TestConnectionString
            / Comments
            Provider=SqlClient";
            var modifiedIni = string.Format("This is an invalid line{0}{1}", Environment.NewLine, ini);
            var iniConfigSrc = new IniFileConfigurationSource(ArbitraryFilePath);
            var outputCacheStream = new MemoryStream();
            iniConfigSrc.Load(StringToStream(ini));

            var exception = Assert.Throws<FormatException>(
                () => iniConfigSrc.Commit(StringToStream(modifiedIni), outputCacheStream));

            Assert.Equal(Resources.FormatError_UnrecognizedLineFormat("This is an invalid line"), exception.Message);
        }

        [Fact]
        public void CommitOperationThrowsExceptionWhenFindNewlyAddedKeyAfterLoadOperation()
        {
            var ini = @"
            ; Comments
            [Data:DefaultConnection]
            # Comments
            ConnectionString=TestConnectionString
            / Comments
            Provider=SqlClient";
            var modifiedIni = string.Format("NewKey1 = NewValue1{0}NewKey2 = NewValue2{0}{1}",
                Environment.NewLine, ini);
            var iniConfigSrc = new IniFileConfigurationSource(ArbitraryFilePath);
            var outputCacheStream = new MemoryStream();
            iniConfigSrc.Load(StringToStream(ini));

            var exception = Assert.Throws<InvalidOperationException>(
                () => iniConfigSrc.Commit(StringToStream(modifiedIni), outputCacheStream));

            Assert.Equal(Resources.FormatError_CommitWhenNewKeyFound("NewKey1"), exception.Message);
        }

        [Fact]
        public void CommitOperationThrowsExceptionWhenKeysAreMissingInConfigFile()
        {
            var ini = @"
            ; Comments
            [Data:DefaultConnection]
            # Comments
            ConnectionString=TestConnectionString
            / Comments
            Provider=SqlClient";
            var modifiedIni = ini.Replace("Provider=SqlClient", string.Empty);
            var iniConfigSrc = new IniFileConfigurationSource(ArbitraryFilePath);
            var outputCacheStream = new MemoryStream();
            iniConfigSrc.Load(StringToStream(ini));

            var exception = Assert.Throws<InvalidOperationException>(
                () => iniConfigSrc.Commit(StringToStream(modifiedIni), outputCacheStream));

            Assert.Equal(Resources.FormatError_CommitWhenKeyMissing("Data:DefaultConnection:Provider"),
                exception.Message);
        }

        [Fact]
        public void CanCreateNewConfig()
        {
            var targetIni = @"Data:DefaultConnection:ConnectionString=TestConnectionString
Data:DefaultConnection:Provider=SqlClient
";
            var iniConfigSrc = new IniFileConfigurationSource(ArbitraryFilePath);
            iniConfigSrc.Set("Data:DefaultConnection:ConnectionString", "TestConnectionString");
            iniConfigSrc.Set("Data:DefaultConnection:Provider", "SqlClient");
            var cacheStream = new MemoryStream();

            iniConfigSrc.GenerateNewConfig(cacheStream);

            Assert.Equal(targetIni, StreamToString(cacheStream));
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

        private static string StreamToString(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            StreamReader reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }

    }
}
