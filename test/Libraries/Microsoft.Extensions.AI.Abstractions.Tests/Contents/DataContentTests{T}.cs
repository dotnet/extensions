﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public abstract class DataContentTests<T>
    where T : DataContent
{
    private static T Create(params object?[] args)
    {
        try
        {
            return (T)Activator.CreateInstance(typeof(T), args)!;
        }
        catch (TargetInvocationException e)
        {
            throw e.InnerException!;
        }
    }

    public T CreateDataContent(Uri uri, string? mediaType = null) => Create(uri, mediaType)!;

#pragma warning disable S3997 // String URI overloads should call "System.Uri" overloads
    public T CreateDataContent(string uriString, string? mediaType = null) => Create(uriString, mediaType)!;
#pragma warning restore S3997

    public T CreateDataContent(ReadOnlyMemory<byte> data, string? mediaType = null) => Create(data, mediaType)!;

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
        Assert.Throws(exception, () => CreateDataContent(path));
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
        Assert.Throws<ArgumentException>("mediaType", () => CreateDataContent("http://localhost/test", type));
    }

    [Theory]
    [InlineData("type/subtype")]
    [InlineData("type/subtype;key=value")]
    [InlineData("type/subtype;key=value;another=value")]
    [InlineData("type/subtype;key=value;another=value;yet_another=value")]
    public void Ctor_ValidMediaType_Roundtrips(string mediaType)
    {
        T content = CreateDataContent("http://localhost/test", mediaType);
        Assert.Equal(mediaType, content.MediaType);

        content = CreateDataContent("data:,", mediaType);
        Assert.Equal(mediaType, content.MediaType);

        content = CreateDataContent("data:text/plain,", mediaType);
        Assert.Equal(mediaType, content.MediaType);

        content = CreateDataContent(new Uri("data:text/plain,"), mediaType);
        Assert.Equal(mediaType, content.MediaType);

        content = CreateDataContent(new byte[] { 0, 1, 2 }, mediaType);
        Assert.Equal(mediaType, content.MediaType);

        content = CreateDataContent(content.Uri);
        Assert.Equal(mediaType, content.MediaType);
    }

    [Fact]
    public void Ctor_NoMediaType_Roundtrips()
    {
        T content;

        foreach (string url in new[] { "http://localhost/test", "about:something", "file://c:\\path" })
        {
            content = CreateDataContent(url);
            Assert.Equal(url, content.Uri);
            Assert.Null(content.MediaType);
            Assert.Null(content.Data);
        }

        content = CreateDataContent("data:,something");
        Assert.Equal("data:,something", content.Uri);
        Assert.Null(content.MediaType);
        Assert.Equal("something"u8.ToArray(), content.Data!.Value.ToArray());

        content = CreateDataContent("data:,Hello+%3C%3E");
        Assert.Equal("data:,Hello+%3C%3E", content.Uri);
        Assert.Null(content.MediaType);
        Assert.Equal("Hello <>"u8.ToArray(), content.Data!.Value.ToArray());
    }

    [Fact]
    public void Serialize_MatchesExpectedJson()
    {
        Assert.Equal(
            """{"uri":"data:,"}""",
            JsonSerializer.Serialize(CreateDataContent("data:,"), TestJsonSerializerContext.Default.Options));

        Assert.Equal(
            """{"uri":"http://localhost/"}""",
            JsonSerializer.Serialize(CreateDataContent(new Uri("http://localhost/")), TestJsonSerializerContext.Default.Options));

        Assert.Equal(
            """{"uri":"data:application/octet-stream;base64,AQIDBA==","mediaType":"application/octet-stream"}""",
            JsonSerializer.Serialize(CreateDataContent(
                uriString: "data:application/octet-stream;base64,AQIDBA=="), TestJsonSerializerContext.Default.Options));

        Assert.Equal(
            """{"uri":"data:application/octet-stream;base64,AQIDBA==","mediaType":"application/octet-stream"}""",
            JsonSerializer.Serialize(CreateDataContent(
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
        Assert.Equal([0x01, 0x02, 0x03, 0x04], content.Data!.Value.ToArray());
        Assert.Equal("application/octet-stream", content.MediaType);
        Assert.True(content.ContainsData);

        // Uri referenced content-only 
        content = JsonSerializer.Deserialize<DataContent>("""{"mediaType":"application/octet-stream","uri":"http://localhost/"}""", TestJsonSerializerContext.Default.Options)!;

        Assert.Null(content.Data);
        Assert.Equal("http://localhost/", content.Uri);
        Assert.Equal("application/octet-stream", content.MediaType);
        Assert.False(content.ContainsData);

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
        Assert.Equal([0x01, 0x02, 0x03, 0x04], content.Data!.Value.ToArray());
        Assert.Equal("text/plain", content.MediaType);
        Assert.True(content.ContainsData);
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
}
