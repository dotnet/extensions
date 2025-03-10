// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public sealed class DataContentTests
{
    [Theory]

    // Invalid URI
    [InlineData("", typeof(ArgumentException))]
    [InlineData("invalid", typeof(ArgumentException))]
    [InlineData("data", typeof(ArgumentException))]

    // Not a data URI
    [InlineData("http://localhost/blah.png", typeof(ArgumentException))]
    [InlineData("https://localhost/blah.png", typeof(ArgumentException))]
    [InlineData("ftp://localhost/blah.png", typeof(ArgumentException))]
    [InlineData("a://localhost/blah.png", typeof(ArgumentException))]

    // Format errors
    [InlineData("data:", typeof(UriFormatException))] // data missing comma
    [InlineData("data:something,", typeof(UriFormatException))] // mime type without subtype
    [InlineData("data:something;else,data", typeof(UriFormatException))] // mime type without subtype
    [InlineData("data:type/subtype;;parameter=value;else,", typeof(UriFormatException))] // parameter without value
    [InlineData("data:type/subtype;parameter=va=lue;else,", typeof(UriFormatException))] // parameter with multiple =
    [InlineData("data:type/subtype;=value;else,", typeof(UriFormatException))] // empty parameter name
    [InlineData("data:image/j/peg;base64,/9j/4AAQSkZJRgABAgAAZABkAAD", typeof(UriFormatException))] // multiple slashes in media type

    // Base64 Validation Errors
    [InlineData("data:text;base64,something!", typeof(UriFormatException))]  // Invalid base64 due to invalid character '!'
    [InlineData("data:text/plain;base64,U29tZQ==\t", typeof(UriFormatException))] // Invalid base64 due to tab character
    [InlineData("data:text/plain;base64,U29tZQ==\r", typeof(UriFormatException))] // Invalid base64 due to carriage return character
    [InlineData("data:text/plain;base64,U29tZQ==\n", typeof(UriFormatException))] // Invalid base64 due to line feed character
    [InlineData("data:text/plain;base64,U29t\r\nZQ==", typeof(UriFormatException))] // Invalid base64 due to carriage return and line feed characters
    [InlineData("data:text/plain;base64,U29", typeof(UriFormatException))] // Invalid base64 due to missing padding
    [InlineData("data:text/plain;base64,U29tZQ", typeof(UriFormatException))] // Invalid base64 due to missing padding
    [InlineData("data:text/plain;base64,U29tZQ=", typeof(UriFormatException))] // Invalid base64 due to missing padding
    public void Ctor_InvalidUri_Throws(string path, Type exception)
    {
        Assert.Throws(exception, () => new DataContent(path));
    }

    [Theory]
    [InlineData("type")]
    [InlineData("type//subtype")]
    [InlineData("type/subtype/")]
    [InlineData("type/subtype;key=")]
    [InlineData("type/subtype;=value")]
    [InlineData("type/subtype;key=value;another=")]
    public void Ctor_InvalidMediaType_Throws(string type)
    {
        Assert.Throws<ArgumentException>("mediaType", () => new DataContent("data:image/png;base64,aGVsbG8=", type));
    }

    [Theory]
    [InlineData("type/subtype")]
    [InlineData("type/subtype;key=value")]
    [InlineData("type/subtype;key=value;another=value")]
    [InlineData("type/subtype;key=value;another=value;yet_another=value")]
    public void Ctor_ValidMediaType_Roundtrips(string mediaType)
    {
        var content = new DataContent("data:image/png;base64,aGVsbG8=", mediaType);
        Assert.Equal(mediaType, content.MediaType);

        content = new DataContent("data:,", mediaType);
        Assert.Equal(mediaType, content.MediaType);

        content = new DataContent("data:text/plain,", mediaType);
        Assert.Equal(mediaType, content.MediaType);

        content = new DataContent(new Uri("data:text/plain,"), mediaType);
        Assert.Equal(mediaType, content.MediaType);

        content = new DataContent(new byte[] { 0, 1, 2 }, mediaType);
        Assert.Equal(mediaType, content.MediaType);

        content = new DataContent(content.Uri);
        Assert.Equal(mediaType, content.MediaType);
    }

    [Fact]
    public void Ctor_NoMediaType_Roundtrips()
    {
        DataContent content;

        content = new DataContent("data:image/png;base64,aGVsbG8=");
        Assert.Equal("data:image/png;base64,aGVsbG8=", content.Uri);
        Assert.Equal("image/png", content.MediaType);

        content = new DataContent(new Uri("data:image/png;base64,aGVsbG8="));
        Assert.Equal("data:image/png;base64,aGVsbG8=", content.Uri);
        Assert.Equal("image/png", content.MediaType);
    }

    [Fact]
    public void Serialize_MatchesExpectedJson()
    {
        Assert.Equal(
            """{"uri":"data:application/octet-stream;base64,AQIDBA=="}""",
            JsonSerializer.Serialize(new DataContent(
                uri: "data:application/octet-stream;base64,AQIDBA=="), TestJsonSerializerContext.Default.Options));

        Assert.Equal(
            """{"uri":"data:application/octet-stream;base64,AQIDBA=="}""",
            JsonSerializer.Serialize(new DataContent(
                new ReadOnlyMemory<byte>([0x01, 0x02, 0x03, 0x04]), "application/octet-stream"),
                TestJsonSerializerContext.Default.Options));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{ "mediaType":"text/plain" }""")]
    public void Deserialize_MissingUriString_Throws(string json)
    {
        Assert.Throws<ArgumentNullException>("uri", () => JsonSerializer.Deserialize<DataContent>(json, TestJsonSerializerContext.Default.Options)!);
    }

    [Fact]
    public void Deserialize_MatchesExpectedData()
    {
        // Data + MimeType only
        var content = JsonSerializer.Deserialize<DataContent>("""{"uri":"data:application/octet-stream;base64,AQIDBA=="}""", TestJsonSerializerContext.Default.Options)!;

        Assert.Equal("data:application/octet-stream;base64,AQIDBA==", content.Uri);
        Assert.Equal([0x01, 0x02, 0x03, 0x04], content.Data.ToArray());
        Assert.Equal("application/octet-stream", content.MediaType);

        // Uri referenced content-only
        content = JsonSerializer.Deserialize<DataContent>("""{"uri":"data:application/octet-stream;base64,AQIDBA=="}""", TestJsonSerializerContext.Default.Options)!;

        Assert.Equal("data:application/octet-stream;base64,AQIDBA==", content.Uri);
        Assert.Equal("application/octet-stream", content.MediaType);

        // Using extra metadata
        content = JsonSerializer.Deserialize<DataContent>("""
            {
                "uri": "data:audio/wav;base64,AQIDBA==",
                "modelId": "gpt-4",
                "additionalProperties":
                {
                    "key": "value"
                }
            }
        """, TestJsonSerializerContext.Default.Options)!;

        Assert.Equal("data:audio/wav;base64,AQIDBA==", content.Uri);
        Assert.Equal([0x01, 0x02, 0x03, 0x04], content.Data.ToArray());
        Assert.Equal("audio/wav", content.MediaType);
        Assert.Equal("value", content.AdditionalProperties!["key"]!.ToString());
    }

    [Theory]
    [InlineData(
        """{"uri": "data:text/plain;base64,AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8="}""",
        """{"uri":"data:text/plain;base64,AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8="}""")]
    [InlineData( // Does not support non-readable content
        """{"uri": "data:text/plain;base64,AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=", "unexpected": true}""",
        """{"uri":"data:text/plain;base64,AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8="}""")]
    public void Serialize_Deserialize_Roundtrips(string serialized, string expectedToString)
    {
        var content = JsonSerializer.Deserialize<DataContent>(serialized, TestJsonSerializerContext.Default.Options)!;
        var reSerialization = JsonSerializer.Serialize(content, TestJsonSerializerContext.Default.Options);
        Assert.Equal(expectedToString, reSerialization);
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("application/octet-stream")]
    [InlineData("application/pdf")]
    [InlineData("application/xml")]
    [InlineData("audio/mpeg")]
    [InlineData("audio/ogg")]
    [InlineData("audio/wav")]
    [InlineData("image/apng")]
    [InlineData("image/avif")]
    [InlineData("image/bmp")]
    [InlineData("image/gif")]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/svg+xml")]
    [InlineData("image/tiff")]
    [InlineData("image/webp")]
    [InlineData("text/css")]
    [InlineData("text/csv")]
    [InlineData("text/html")]
    [InlineData("text/javascript")]
    [InlineData("text/plain")]
    [InlineData("text/plain;charset=UTF-8")]
    [InlineData("text/xml")]
    [InlineData("custom/mediatypethatdoesntexists")]
    public void MediaType_Roundtrips(string mediaType)
    {
        DataContent c = new("data:,", mediaType);
        Assert.Equal(mediaType, c.MediaType);
    }

    [Theory]
    [InlineData("image/gif", "image/")]
    [InlineData("IMAGE/JPEG", "image")]
    [InlineData("image/vnd.microsoft.icon", "ima")]
    [InlineData("image/svg+xml", "IMAGE/")]
    [InlineData("image/nonexistentimagemimetype", "IMAGE")]
    [InlineData("audio/mpeg", "aUdIo/")]
    [InlineData("application/json", "")]
    [InlineData("application/pdf", "application/pdf")]
    public void HasMediaTypePrefix_ReturnsTrue(string mediaType, string prefix)
    {
        var content = new DataContent("data:application/octet-stream;base64,AQIDBA==", mediaType);
        Assert.True(content.MediaTypeStartsWith(prefix));
    }

    [Theory]
    [InlineData("audio/mpeg", "image/")]
    [InlineData("text/css", "text/csv")]
    [InlineData("text/css", "/csv")]
    [InlineData("application/json", "application/json!")]
    public void HasMediaTypePrefix_ReturnsFalse(string mediaType, string prefix)
    {
        var content = new DataContent("data:application/octet-stream;base64,AQIDBA==", mediaType);
        Assert.False(content.MediaTypeStartsWith(prefix));
    }
}
