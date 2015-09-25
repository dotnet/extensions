// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Framework.Configuration.Json;
using Microsoft.Framework.Configuration.Test;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Framework.Configuration
{
    public class JsonConfigurationTest
    {
        [Fact]
        public void LoadKeyValuePairsFromValidJson()
        {
            var json = @"
{
    'firstname': 'test',
    'test.last.name': 'last.name',
        'residential.address': {
            'street.name': 'Something street',
            'zipcode': '12345'
        }
}";
            var jsonConfigSrc = new JsonConfigurationProvider(TestStreamHelpers.ArbitraryFilePath);

            jsonConfigSrc.Load(TestStreamHelpers.StringToStream(json));

            Assert.Equal("test", jsonConfigSrc.Get("firstname"));
            Assert.Equal("last.name", jsonConfigSrc.Get("test.last.name"));
            Assert.Equal("Something street", jsonConfigSrc.Get("residential.address:STREET.name"));
            Assert.Equal("12345", jsonConfigSrc.Get("residential.address:zipcode"));
        }

        [Fact]
        public void LoadMethodCanHandleEmptyValue()
        {
            var json = @"
{
    'name': ''
}";
            var jsonConfigSrc = new JsonConfigurationProvider(TestStreamHelpers.ArbitraryFilePath);

            jsonConfigSrc.Load(TestStreamHelpers.StringToStream(json));

            Assert.Equal(string.Empty, jsonConfigSrc.Get("name"));
        }

        [Fact]
        public void NonObjectRootIsInvalid()
        {
            var json = @"'test'";
            var jsonConfigSource = new JsonConfigurationProvider(TestStreamHelpers.ArbitraryFilePath);
          
            var exception = Assert.Throws<FormatException>(
                () => jsonConfigSource.Load(TestStreamHelpers.StringToStream(json)));

            Assert.NotNull(exception.Message);
        }

        [Fact]
        public void SupportAndIgnoreComments()
        {
            var json = @"/* Comments */
                {/* Comments */
                ""name"": /* Comments */ ""test"",
                ""address"": {
                    ""street"": ""Something street"", /* Comments */
                    ""zipcode"": ""12345""
                }
            }";
            var jsonConfigSrc = new JsonConfigurationProvider(TestStreamHelpers.ArbitraryFilePath);

            jsonConfigSrc.Load(TestStreamHelpers.StringToStream(json));

            Assert.Equal("test", jsonConfigSrc.Get("name"));
            Assert.Equal("Something street", jsonConfigSrc.Get("address:street"));
            Assert.Equal("12345", jsonConfigSrc.Get("address:zipcode"));
        }

        [Fact]
        public void ThrowExceptionWhenUnexpectedEndFoundBeforeFinishParsing()
        {
            var json = @"{
                'name': 'test',
                'address': {
                    'street': 'Something street',
                    'zipcode': '12345'
                }
            /* Missing a right brace here*/";
            var jsonConfigSource = new JsonConfigurationProvider(TestStreamHelpers.ArbitraryFilePath);
           
            var exception = Assert.Throws<FormatException>(
                () => jsonConfigSource.Load(TestStreamHelpers.StringToStream(json)));
            Assert.NotNull(exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenPassingNullAsFilePath()
        {
            var expectedMsg = new ArgumentException(Resources.Error_InvalidFilePath, "path").Message;

            var exception = Assert.Throws<ArgumentException>(() => new JsonConfigurationProvider(null));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenPassingEmptyStringAsFilePath()
        {
            var expectedMsg = new ArgumentException(Resources.Error_InvalidFilePath, "path").Message;

            var exception = Assert.Throws<ArgumentException>(() => new JsonConfigurationProvider(string.Empty));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void JsonConfiguration_Throws_On_Missing_Configuration_File()
        {
            var configSource = new JsonConfigurationProvider("NotExistingConfig.json", optional: false);
            var exception = Assert.Throws<FileNotFoundException>(() => configSource.Load());

            // Assert
            Assert.Equal(Resources.FormatError_FileNotFound("NotExistingConfig.json"), exception.Message);
        }

        [Fact]
        public void JsonConfiguration_Does_Not_Throw_On_Optional_Configuration()
        {
            var configSource = new JsonConfigurationProvider("NotExistingConfig.json", optional: true);
            configSource.Load();
            Assert.Throws<InvalidOperationException>(() => configSource.Get("key"));
        }
    }
}