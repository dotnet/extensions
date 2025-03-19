// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class EmbeddingGeneratorMetadataTests
{
    [Fact]
    public async Task Constructor_NullValues_AllowedAndRoundtrip()
    {
        EmbeddingGeneratorMetadata providerMetadata = new(null, null, null);
        Assert.Null(providerMetadata.ProviderName);
        Assert.Null(providerMetadata.ProviderUri);
        Assert.Null(providerMetadata.DefaultModelId);

        var defaultModelMetadata = await providerMetadata.GetModelMetadataAsync();
        Assert.Null(defaultModelMetadata.Dimensions);

        var unknownModelMetadata = await providerMetadata.GetModelMetadataAsync("some unknown model ID");
        Assert.Null(unknownModelMetadata.Dimensions);
    }

    [Fact]
    public async Task Constructor_Value_Roundtrips()
    {
        var uri = new Uri("https://example.com");
        EmbeddingGeneratorMetadata metadata = new TestEmbeddingGeneratorMetadata("providerName", uri, "theModel", 42);
        Assert.Equal("providerName", metadata.ProviderName);
        Assert.Same(uri, metadata.ProviderUri);
        Assert.Equal("theModel", metadata.DefaultModelId);
        Assert.Equal(42, (await metadata.GetModelMetadataAsync()).Dimensions);
    }

    private class TestEmbeddingGeneratorMetadata(string providerName, Uri providerUri, string modelId, int dimensions)
        : EmbeddingGeneratorMetadata(providerName, providerUri, modelId)
    {
        public override Task<EmbeddingModelMetadata> GetModelMetadataAsync(string? modelId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new EmbeddingModelMetadata { Dimensions = dimensions });
    }
}
