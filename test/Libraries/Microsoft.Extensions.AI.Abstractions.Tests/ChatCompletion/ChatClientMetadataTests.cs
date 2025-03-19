// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ChatClientMetadataTests
{
    [Fact]
    public async Task Constructor_NullValues_AllowedAndRoundtrip()
    {
        ChatClientMetadata providerMetadata = new(null, null, null);
        Assert.Null(providerMetadata.ProviderName);
        Assert.Null(providerMetadata.ProviderUri);
        Assert.Null(providerMetadata.DefaultModelId);

        var modelMetadata = await providerMetadata.GetModelMetadataAsync();
        Assert.Null(modelMetadata.SupportsNativeJsonSchema);
    }

    [Fact]
    public async Task Constructor_Value_Roundtrips()
    {
        var uri = new Uri("https://example.com");
        TestChatClientMetadata providerMetadata = new("providerName", uri, "theModel", true);
        Assert.Equal("providerName", providerMetadata.ProviderName);
        Assert.Same(uri, providerMetadata.ProviderUri);
        Assert.Equal("theModel", providerMetadata.DefaultModelId);

        var modelMetadata = await providerMetadata.GetModelMetadataAsync();
        Assert.True(modelMetadata.SupportsNativeJsonSchema);
    }

    private class TestChatClientMetadata(string providerName, Uri providerUri, string modelId, bool? supportsNativeJsonSchema)
        : ChatClientMetadata(providerName, providerUri, modelId)
    {
        public override Task<ChatModelMetadata> GetModelMetadataAsync(string? modelId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new ChatModelMetadata { SupportsNativeJsonSchema = supportsNativeJsonSchema });
    }
}
