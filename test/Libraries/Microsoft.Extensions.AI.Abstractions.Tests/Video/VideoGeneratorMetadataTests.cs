// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.AI;

public class VideoGeneratorMetadataTests
{
    [Fact]
    public void Constructor_NullValues()
    {
        var metadata = new VideoGeneratorMetadata();
        Assert.Null(metadata.ProviderName);
        Assert.Null(metadata.ProviderUri);
        Assert.Null(metadata.DefaultModelId);
    }

    [Fact]
    public void Constructor_WithValues()
    {
        var uri = new Uri("https://api.example.com/v1");
        var metadata = new VideoGeneratorMetadata("test-provider", uri, "sora");
        Assert.Equal("test-provider", metadata.ProviderName);
        Assert.Equal(uri, metadata.ProviderUri);
        Assert.Equal("sora", metadata.DefaultModelId);
    }
}
