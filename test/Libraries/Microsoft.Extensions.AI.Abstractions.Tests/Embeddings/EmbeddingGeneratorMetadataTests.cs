// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.AI;

public class EmbeddingGeneratorMetadataTests
{
    [Fact]
    public void Constructor_NullValues_AllowedAndRoundtrip()
    {
        EmbeddingGeneratorMetadata metadata = new(null, null, null, null);
        Assert.Null(metadata.ProviderName);
        Assert.Null(metadata.ProviderUri);
        Assert.Null(metadata.ModelId);
        Assert.Null(metadata.Dimensions);
    }

    [Fact]
    public void Constructor_Value_Roundtrips()
    {
        var uri = new Uri("https://example.com");
        EmbeddingGeneratorMetadata metadata = new("providerName", uri, "theModel", 42);
        Assert.Equal("providerName", metadata.ProviderName);
        Assert.Same(uri, metadata.ProviderUri);
        Assert.Equal("theModel", metadata.ModelId);
        Assert.Equal(42, metadata.Dimensions);
    }
}
