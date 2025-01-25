// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;
using Xunit;

namespace Microsoft.Shared.AIExtensions.Test;

public class DataContentExtensionsTest
{
    [Theory]
    [InlineData("image/apng")]
    [InlineData("image/avif")]
    [InlineData("image/bmp")]
    [InlineData("image/gif")]
    [InlineData("image/vnd.microsoft.icon")]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/svg+xml")]
    [InlineData("image/tiff")]
    [InlineData("image/webp")]
    [InlineData("image/nonexistentimagemimetype")]
    public void DataContentExtensions_HasImageMediaType_ReturnsTrue(string? mediaType)
    {
        var content = new DataContent("http://localhost/image.png", mediaType);
        Assert.True(content.HasImageMediaType());
    }

    [Theory]
    [InlineData("audio/mpeg")]
    [InlineData("text/css")]
    [InlineData("text/html")]
    [InlineData("text/javscript")]
    [InlineData("text/plain")]
    [InlineData("application/json")]
    [InlineData("application/pdf")]
    [InlineData("video/mpeg")]
    [InlineData("font/otf")]
    [InlineData("")]
    [InlineData(null)]
    public void DataContentExtensions_HasImageMediaType_ReturnsFalse(string? mediaType)
    {
        var content = new DataContent("http://localhost/image.png", mediaType);

        Assert.False(content.HasImageMediaType());
    }
}
