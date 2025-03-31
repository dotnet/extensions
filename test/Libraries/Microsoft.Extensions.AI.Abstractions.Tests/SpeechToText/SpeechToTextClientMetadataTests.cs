// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.AI;

public class SpeechToTextClientMetadataTests
{
    [Fact]
    public void Constructor_NullValues_AllowedAndRoundtrip()
    {
        SpeechToTextClientMetadata metadata = new(null, null, null);
        Assert.Null(metadata.ProviderName);
        Assert.Null(metadata.ProviderUri);
        Assert.Null(metadata.DefaultModelId);
    }

    [Fact]
    public void Constructor_Value_Roundtrips()
    {
        var uri = new Uri("https://example.com");
        SpeechToTextClientMetadata metadata = new("providerName", uri, "theModel");
        Assert.Equal("providerName", metadata.ProviderName);
        Assert.Same(uri, metadata.ProviderUri);
        Assert.Equal("theModel", metadata.DefaultModelId);
    }
}
