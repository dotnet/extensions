// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public sealed class UriContentTests
{
    [Fact]
    public void Ctor_InvalidUriMediaType_Throws()
    {
        Assert.Throws<ArgumentNullException>("uri", () => new UriContent((string)null!, "image/png"));
        Assert.Throws<ArgumentNullException>("uri", () => new UriContent((Uri)null!, "image/png"));
        Assert.Throws<UriFormatException>(() => new UriContent("notauri", "image/png"));

        Assert.Throws<ArgumentNullException>("mediaType", () => new UriContent("data:image/png;base64,aGVsbG8=", null!));
        Assert.Throws<ArgumentException>("mediaType", () => new UriContent("data:image/png;base64,aGVsbG8=", ""));
        Assert.Throws<ArgumentException>("mediaType", () => new UriContent("data:image/png;base64,aGVsbG8=", "image"));

        Assert.Throws<ArgumentNullException>("mediaType", () => new UriContent(new Uri("data:image/png;base64,aGVsbG8="), null!));
        Assert.Throws<ArgumentException>("mediaType", () => new UriContent(new Uri("data:image/png;base64,aGVsbG8="), ""));
        Assert.Throws<ArgumentException>("mediaType", () => new UriContent(new Uri("data:image/png;base64,aGVsbG8="), "audio"));

        UriContent c = new("http://localhost/something", "image/png");
        Assert.Throws<ArgumentNullException>("value", () => c.Uri = null!);
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
        Assert.Throws<ArgumentException>("mediaType", () => new UriContent("http://localhost/something", type));

        UriContent c = new("http://localhost/something", "image/png");
        Assert.Throws<ArgumentException>("value", () => c.MediaType = type);
        Assert.Throws<ArgumentNullException>("value", () => c.MediaType = null!);
    }

    [Theory]
    [InlineData("type/subtype")]
    [InlineData("type/subtype;key=value")]
    [InlineData("type/subtype;key=value;another=value")]
    [InlineData("type/subtype;key=value;another=value;yet_another=value")]
    public void Ctor_ValidMediaType_Roundtrips(string mediaType)
    {
        var content = new UriContent("http://localhost/something", mediaType);
        Assert.Equal(mediaType, content.MediaType);

        content.MediaType = "image/png";
        Assert.Equal("image/png", content.MediaType);

        content.MediaType = mediaType;
        Assert.Equal(mediaType, content.MediaType);
    }

    [Fact]
    public void Serialize_MatchesExpectedJson()
    {
        Assert.Equal(
            """{"uri":"http://localhost/something","mediaType":"image/png"}""",
            JsonSerializer.Serialize(
                new UriContent("http://localhost/something", "image/png"),
                TestJsonSerializerContext.Default.Options));
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
        UriContent c = new("http://localhost", mediaType);
        Assert.Equal(mediaType, c.MediaType);
    }

    [Theory]
    [InlineData("image/gif", "image")]
    [InlineData("IMAGE/JPEG", "image")]
    [InlineData("image/vnd.microsoft.icon", "imAge")]
    [InlineData("image/svg+xml", "IMAGE")]
    [InlineData("image/nonexistentimagemimetype", "IMAGE")]
    [InlineData("audio/mpeg", "aUdIo")]
    public void HasMediaTypePrefix_ReturnsTrue(string mediaType, string prefix)
    {
        var content = new UriContent("http://localhost", mediaType);
        Assert.True(content.HasTopLevelMediaType(prefix));
    }

    [Theory]
    [InlineData("audio/mpeg", "audio/")]
    [InlineData("audio/mpeg", "image")]
    [InlineData("audio/mpeg", "audio/mpeg")]
    [InlineData("text/css", "text/csv")]
    [InlineData("text/css", "/csv")]
    [InlineData("application/json", "application/json!")]
    public void HasMediaTypePrefix_ReturnsFalse(string mediaType, string prefix)
    {
        var content = new UriContent("http://localhost", mediaType);
        Assert.False(content.HasTopLevelMediaType(prefix));
    }
}
