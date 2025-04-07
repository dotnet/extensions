// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ChatClientMetadataTests
{
    [Fact]
    public void Constructor_NullValues_AllowedAndRoundtrip()
    {
        ChatClientMetadata providerMetadata = new(null, null, null);
        Assert.Null(providerMetadata.ProviderName);
        Assert.Null(providerMetadata.ProviderUri);
        Assert.Null(providerMetadata.DefaultModelId);
    }

    [Fact]
    public void Constructor_Value_Roundtrips()
    {
        var uri = new Uri("https://example.com");
        ChatClientMetadata providerMetadata = new("providerName", uri, "theModel");
        Assert.Equal("providerName", providerMetadata.ProviderName);
        Assert.Same(uri, providerMetadata.ProviderUri);
        Assert.Equal("theModel", providerMetadata.DefaultModelId);
    }
}
