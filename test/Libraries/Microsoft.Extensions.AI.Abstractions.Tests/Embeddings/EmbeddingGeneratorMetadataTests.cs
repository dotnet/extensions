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
        EmbeddingGeneratorMetadata providerMetadata = new(null, null, null);
        Assert.Null(providerMetadata.ProviderName);
        Assert.Null(providerMetadata.ProviderUri);
        Assert.Null(providerMetadata.DefaultModelId);
        Assert.Null(providerMetadata.DefaultModelDimensions);
    }

    [Fact]
    public void Constructor_Value_Roundtrips()
    {
        var uri = new Uri("https://example.com");
        EmbeddingGeneratorMetadata metadata = new EmbeddingGeneratorMetadata("providerName", uri, "theModel", 42);
        Assert.Equal("providerName", metadata.ProviderName);
        Assert.Same(uri, metadata.ProviderUri);
        Assert.Equal("theModel", metadata.DefaultModelId);
        Assert.Equal(42, metadata.DefaultModelDimensions);
    }
}
