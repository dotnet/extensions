// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Formatters;
using Xunit;

namespace Microsoft.AspNetCore.Diagnostics.Logging.Test;

public class MediaTypeSetExtensionsTests
{
    [Fact]
    public void Covers_WhenCovers_ReturnsTrue()
    {
        var collection = new[]
            {
                new MediaType("application/xml"),
                new MediaType("text/*")
            };

        Assert.True(collection.Covers("application/xml"));
        Assert.True(collection.Covers("text/whatever"));
        Assert.True(collection.Covers("text/whatever-else"));
    }

    [Fact]
    public void Covers_WhenNotCovers_ReturnsFalse()
    {
        var collection = new[]
            {
                new MediaType("application/xml"),
                new MediaType("text/*")
            };

        Assert.False(collection.Covers(null));
        Assert.False(collection.Covers(string.Empty));
        Assert.False(collection.Covers("image"));
        Assert.False(collection.Covers("image/png"));
        Assert.False(collection.Covers("audio/ogg"));
        Assert.False(collection.Covers("application"));
        Assert.False(collection.Covers("application/octet-stream"));
    }
}
