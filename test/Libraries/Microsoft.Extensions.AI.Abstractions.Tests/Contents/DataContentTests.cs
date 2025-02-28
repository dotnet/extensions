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
    [InlineData("invalid", typeof(UriFormatException))]

    // Format errors
    [InlineData("data", typeof(UriFormatException))] // data missing colon
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
        Assert.Throws<ArgumentException>("mediaType", () => new DataContent("http://localhost/test", type));
    }

    [Theory]
    [InlineData("type/subtype")]
    [InlineData("type/subtype;key=value")]
    [InlineData("type/subtype;key=value;another=value")]
    [InlineData("type/subtype;key=value;another=value;yet_another=value")]
    public void Ctor_ValidMediaType_Roundtrips(string mediaType)
    {
        var content = new DataContent("http://localhost/test", mediaType);
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

        foreach (string url in new[] { "http://localhost/test", "about:something", "file://c:\\path" })
        {
            content = new DataContent(url);
            Assert.Equal(url, content.Uri);
            Assert.Null(content.MediaType);
            Assert.Null(content.Data);
        }

        content = new DataContent("data:,something");
        Assert.Equal("data:,something", content.Uri);
        Assert.Null(content.MediaType);
        Assert.Equal("something"u8.ToArray(), content.Data!.Value.ToArray());

        content = new DataContent("data:,Hello+%3C%3E");
        Assert.Equal("data:,Hello+%3C%3E", content.Uri);
        Assert.Null(content.MediaType);
        Assert.Equal("Hello <>"u8.ToArray(), content.Data!.Value.ToArray());
    }

    [Fact]
    public void Serialize_MatchesExpectedJson()
    {
        Assert.Equal(
            """{"uri":"data:,"}""",
            JsonSerializer.Serialize(new DataContent("data:,"), TestJsonSerializerContext.Default.Options));

        Assert.Equal(
            """{"uri":"http://localhost/"}""",
            JsonSerializer.Serialize(new DataContent(new Uri("http://localhost/")), TestJsonSerializerContext.Default.Options));

        Assert.Equal(
            """{"uri":"data:application/octet-stream;base64,AQIDBA==","mediaType":"application/octet-stream"}""",
            JsonSerializer.Serialize(new DataContent(
                uri: "data:application/octet-stream;base64,AQIDBA=="), TestJsonSerializerContext.Default.Options));

        Assert.Equal(
            """{"uri":"data:application/octet-stream;base64,AQIDBA==","mediaType":"application/octet-stream"}""",
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
        var content = JsonSerializer.Deserialize<DataContent>("""{"mediaType":"application/octet-stream","uri":"data:;base64,AQIDBA=="}""", TestJsonSerializerContext.Default.Options)!;

        Assert.Equal("data:application/octet-stream;base64,AQIDBA==", content.Uri);
        Assert.NotNull(content.Data);
        Assert.Equal([0x01, 0x02, 0x03, 0x04], content.Data.Value.ToArray());
        Assert.Equal("application/octet-stream", content.MediaType);

        // Uri referenced content-only
        content = JsonSerializer.Deserialize<DataContent>("""{"mediaType":"application/octet-stream","uri":"http://localhost/"}""", TestJsonSerializerContext.Default.Options)!;

        Assert.Null(content.Data);
        Assert.Equal("http://localhost/", content.Uri);
        Assert.Equal("application/octet-stream", content.MediaType);

        // Using extra metadata
        content = JsonSerializer.Deserialize<DataContent>("""
            {
                "uri": "data:;base64,AQIDBA==",
                "modelId": "gpt-4",
                "additionalProperties":
                {
                    "key": "value"
                },
                "mediaType": "text/plain"
            }
        """, TestJsonSerializerContext.Default.Options)!;

        Assert.Equal("data:text/plain;base64,AQIDBA==", content.Uri);
        Assert.NotNull(content.Data);
        Assert.Equal([0x01, 0x02, 0x03, 0x04], content.Data.Value.ToArray());
        Assert.Equal("text/plain", content.MediaType);
        Assert.Equal("value", content.AdditionalProperties!["key"]!.ToString());
    }

    [Theory]
    [InlineData(
        """{"uri": "data:;base64,AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=","mediaType": "text/plain"}""",
        """{"uri":"data:text/plain;base64,AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=","mediaType":"text/plain"}""")]
    [InlineData(
        """{"uri": "data:text/plain;base64,AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=","mediaType": "text/plain"}""",
        """{"uri":"data:text/plain;base64,AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=","mediaType":"text/plain"}""")]
    [InlineData( // Does not support non-readable content
        """{"uri": "data:text/plain;base64,AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=", "unexpected": true}""",
        """{"uri":"data:text/plain;base64,AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=","mediaType":"text/plain"}""")]
    [InlineData( // Uri comes before mimetype
        """{"mediaType": "text/plain", "uri": "http://localhost/" }""",
        """{"uri":"http://localhost/","mediaType":"text/plain"}""")]
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
    public void HasMediaTypePrefix_ReturnsTrue(string? mediaType, string prefix)
    {
        var content = new DataContent("http://localhost/image.png", mediaType);
        Assert.True(content.MediaTypeStartsWith(prefix));
    }

    [Theory]
    [InlineData("audio/mpeg", "image/")]
    [InlineData("text/css", "text/csv")]
    [InlineData("application/json", "application/json!")]
    [InlineData("", "")] // The media type will get normalized to null
    [InlineData(null, "image/")]
    [InlineData(null, "")]
    public void HasMediaTypePrefix_ReturnsFalse(string? mediaType, string prefix)
    {
        var content = new DataContent("http://localhost/image.png", mediaType);
        Assert.False(content.MediaTypeStartsWith(prefix));
    }
}
