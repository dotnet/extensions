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

        Assert.Throws<ArgumentException>("mediaType", () => new UriContent("data:image/png;base64,aGVsbG8=", ""));
        Assert.Throws<ArgumentException>("mediaType", () => new UriContent("data:image/png;base64,aGVsbG8=", "image"));

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

    [Theory]
    [InlineData("http://localhost/image.png", "image/png")]
    [InlineData("http://localhost/audio.mp3", "audio/mpeg")]
    [InlineData("http://localhost/document.pdf", "application/pdf")]
    [InlineData("http://localhost/data.json", "application/json")]
    [InlineData("http://localhost/page.html", "text/html")]
    [InlineData("http://localhost/photo.jpg", "image/jpeg")]
    [InlineData("http://localhost/path/to/file.wav", "audio/wav")]
    [InlineData("http://localhost/path/to/file.svg", "image/svg+xml")]
    [InlineData("http://localhost/image.png?width=100&height=100", "image/png")]
    [InlineData("http://localhost/image.png#section", "image/png")]
    [InlineData("http://localhost/image.png?q=1#frag", "image/png")]
    public void Ctor_NullMediaType_InfersFromExtension_StringUri(string uri, string expectedMediaType)
    {
        var content = new UriContent(uri);
        Assert.Equal(expectedMediaType, content.MediaType);

        var content2 = new UriContent(uri, null);
        Assert.Equal(expectedMediaType, content2.MediaType);
    }

    [Theory]
    [InlineData("http://localhost/image.png", "image/png")]
    [InlineData("http://localhost/audio.mp3", "audio/mpeg")]
    [InlineData("http://localhost/document.pdf", "application/pdf")]
    [InlineData("http://localhost/photo.jpg", "image/jpeg")]
    [InlineData("http://localhost/image.png?width=100", "image/png")]
    [InlineData("http://localhost/image.png#section", "image/png")]
    [InlineData("http://localhost/image.png?q=1#frag", "image/png")]
    public void Ctor_NullMediaType_InfersFromExtension_AbsoluteUri(string uri, string expectedMediaType)
    {
        var content = new UriContent(new Uri(uri));
        Assert.Equal(expectedMediaType, content.MediaType);

        var content2 = new UriContent(new Uri(uri), null);
        Assert.Equal(expectedMediaType, content2.MediaType);
    }

    [Theory]
    [InlineData("image.png", "image/png")]
    [InlineData("audio.mp3", "audio/mpeg")]
    [InlineData("path/to/document.pdf", "application/pdf")]
    [InlineData("photo.jpg", "image/jpeg")]
    [InlineData("image.png?width=100", "image/png")]
    [InlineData("image.png#section", "image/png")]
    [InlineData("image.png?q=1#frag", "image/png")]
    [InlineData("path/to/file.wav?key=value&other=123", "audio/wav")]
    [InlineData("path/to/file.svg#top", "image/svg+xml")]
    public void Ctor_NullMediaType_InfersFromExtension_RelativeUri(string uri, string expectedMediaType)
    {
        var content = new UriContent(new Uri(uri, UriKind.Relative));
        Assert.Equal(expectedMediaType, content.MediaType);

        var content2 = new UriContent(new Uri(uri, UriKind.Relative), null);
        Assert.Equal(expectedMediaType, content2.MediaType);
    }

    [Theory]
    [InlineData("http://localhost/noextension")]
    [InlineData("http://localhost/path/to/resource")]
    [InlineData("http://localhost/")]
    [InlineData("http://localhost/file.unknownext")]
    [InlineData("http://localhost/file.xyz123")]
    public void Ctor_NullMediaType_NoOrUnknownExtension_DefaultsToOctetStream(string uri)
    {
        var content = new UriContent(uri);
        Assert.Equal("application/octet-stream", content.MediaType);
    }

    [Theory]
    [InlineData("noextension")]
    [InlineData("path/to/resource")]
    [InlineData("file.unknownext")]
    [InlineData("file.xyz123")]
    [InlineData("noext?q=1")]
    [InlineData("noext#frag")]
    public void Ctor_NullMediaType_RelativeUri_NoOrUnknownExtension_DefaultsToOctetStream(string uri)
    {
        var content = new UriContent(new Uri(uri, UriKind.Relative));
        Assert.Equal("application/octet-stream", content.MediaType);
    }

    [Fact]
    public void Ctor_ExplicitMediaType_OverridesInference()
    {
        var content = new UriContent("http://localhost/image.png", "application/octet-stream");
        Assert.Equal("application/octet-stream", content.MediaType);

        var relContent = new UriContent(new Uri("image.png", UriKind.Relative), "application/octet-stream");
        Assert.Equal("application/octet-stream", relContent.MediaType);
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

    [Fact]
    public void JsonDeserialization_KnownPayload()
    {
        const string Json = """
            {
              "$type": "uri",
              "uri": "http://localhost/something",
              "mediaType": "image/png",
              "additionalProperties": {
                "title": "My Image"
              }
            }
            """;

        AIContent? result = JsonSerializer.Deserialize<AIContent>(Json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(result);
        var uriContent = Assert.IsType<UriContent>(result);
        Assert.Equal(new Uri("http://localhost/something"), uriContent.Uri);
        Assert.Equal("image/png", uriContent.MediaType);
        Assert.NotNull(uriContent.AdditionalProperties);
        Assert.Equal("My Image", uriContent.AdditionalProperties["title"]?.ToString());
    }
}
