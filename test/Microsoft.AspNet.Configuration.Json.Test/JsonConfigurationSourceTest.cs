using System;
using System.IO;
using Xunit;

namespace Microsoft.AspNet.Configuration.Json.Test
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
            var jsonConfigSrc = new JsonConfigurationSource("ThisFilePathWillNotBeUsed");

            jsonConfigSrc.Load(StringToStream(json));

            Assert.Equal(3, jsonConfigSrc.Data.Count);
            Assert.Equal("test", jsonConfigSrc.Data["name"]);
            Assert.Equal("Something street", jsonConfigSrc.Data["address:street"]);
            Assert.Equal("12345", jsonConfigSrc.Data["address:zipcode"]);
        }

        [Fact]
        public void NonObjectRootIsInvalid()
        {
            var json = @"'test'";
            var jsonConfigSource = new JsonConfigurationSource(ArbitraryFilePath);

            Assert.Throws<FormatException>(() => jsonConfigSource.Load(StringToStream(json)));
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

            Assert.Throws<FormatException>(() => jsonConfigSource.Load(StringToStream(json)));
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

            Assert.Throws<FormatException>(() => jsonConfigSource.Load(StringToStream(json)));
        }

        [Fact]
        public void ThrowExceptionWhenPassingNullAsFilePath()
        {
            Assert.Throws<ArgumentException>(() => new JsonConfigurationSource(null));
        }

        [Fact]
        public void ThrowExceptionWhenPassingEmptyStringAsFilePath()
        {
            Assert.Throws<ArgumentException>(() => new JsonConfigurationSource(string.Empty));
        }

        [Fact]
        public void ThrowExceptionWhenPassingWhiteSpacesAsFilePath()
        {
            Assert.Throws<ArgumentException>(() => new JsonConfigurationSource(" \t\n\r"));
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
            var jsonConfigSrc = new JsonConfigurationSource("ThisFilePathWillNotBeUsed");

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
