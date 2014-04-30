using System;
using System.IO;
using Microsoft.AspNet.ConfigurationModel.Sources;
using Xunit;

using Resources = Microsoft.AspNet.ConfigurationModel.Json.Resources;

namespace Microsoft.AspNet.ConfigurationModel.Sources
{
    public class JsonConfigurationSourceTest
    {
        private static readonly string ArbitraryFilePath = "Unit tests do not touch file system";

        [Fact]
        public void LoadKeyValuePairsFromValidJson()
        {
            var json = @"{
                'name': 'test',
                'address': {
                    'street': 'Something street',
                    'zipcode': '12345'
                }
            }";
            var jsonConfigSrc = new JsonConfigurationSource(ArbitraryFilePath);

            jsonConfigSrc.Load(StringToStream(json));

            Assert.Equal(3, jsonConfigSrc.Data.Count);
            Assert.Equal("test", jsonConfigSrc.Data["NAME"]);
            Assert.Equal("Something street", jsonConfigSrc.Data["address:STREET"]);
            Assert.Equal("12345", jsonConfigSrc.Data["address:zipcode"]);
        }

        [Fact]
        public void NonObjectRootIsInvalid()
        {
            var json = @"'test'";
            var jsonConfigSource = new JsonConfigurationSource(ArbitraryFilePath);
            var expectedMsg = Resources.FormatError_RootMustBeAnObject(string.Empty, 1, 6);

            var exception = Assert.Throws<FormatException>(() => jsonConfigSource.Load(StringToStream(json)));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void SupportAndIgnoreComments()
        {
            var json = @" /* Comments */
                {/* Comments */
                'name': /* Comments */ 'test',
                'address': {
                    'street': 'Something street', /* Comments */
                    'zipcode': '12345'
                }
            }";
            var jsonConfigSrc = new JsonConfigurationSource(ArbitraryFilePath);

            jsonConfigSrc.Load(StringToStream(json));

            Assert.Equal(3, jsonConfigSrc.Data.Count);
            Assert.Equal("test", jsonConfigSrc.Data["name"]);
            Assert.Equal("Something street", jsonConfigSrc.Data["address:street"]);
            Assert.Equal("12345", jsonConfigSrc.Data["address:zipcode"]);
        }

        [Fact]
        public void ArraysAreNotSupported()
        {
            var json = @"{
                'name': 'test',
                'address': ['Something street', '12345']
            }";
            var jsonConfigSource = new JsonConfigurationSource(ArbitraryFilePath);
            var expectedMsg = Resources.FormatError_UnsupportedJSONToken("StartArray", "address", 3, 29);

            var exception = Assert.Throws<FormatException>(() => jsonConfigSource.Load(StringToStream(json)));

            Assert.Equal(expectedMsg, exception.Message);
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
            var jsonConfigSource = new JsonConfigurationSource(ArbitraryFilePath);
            var expectedMsg = Resources.FormatError_UnexpectedEnd("address", 7, 44);

            var exception = Assert.Throws<FormatException>(() => jsonConfigSource.Load(StringToStream(json)));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenPassingNullAsFilePath()
        {
            var expectedMsg = new ArgumentException(Resources.Error_InvalidFilePath, "path").Message;

            var exception = Assert.Throws<ArgumentException>(() => new JsonConfigurationSource(null));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenPassingEmptyStringAsFilePath()
        {
            var expectedMsg = new ArgumentException(Resources.Error_InvalidFilePath, "path").Message;

            var exception = Assert.Throws<ArgumentException>(() => new JsonConfigurationSource(string.Empty));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void OverrideValueWhenKeyIsDuplicated()
        {
            var json = @"{
                'name': 'test',
                'address': {
                    'street': 'Something street',
                    'zipcode': '12345'
                },
                'name': 'new name'
            }";
            var jsonConfigSrc = new JsonConfigurationSource(ArbitraryFilePath);

            jsonConfigSrc.Load(StringToStream(json));

            Assert.Equal(3, jsonConfigSrc.Data.Count);
            Assert.Equal("new name", jsonConfigSrc.Data["name"]);
            Assert.Equal("Something street", jsonConfigSrc.Data["address:street"]);
            Assert.Equal("12345", jsonConfigSrc.Data["address:zipcode"]);
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
