// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.AI;

public class AudioTranscriptionClientMetadataTests
{
    [Fact]
    public void Constructor_NullValues_AllowedAndRoundtrip()
    {
        AudioTranscriptionClientMetadata metadata = new(null, null, null);
        Assert.Null(metadata.ProviderName);
        Assert.Null(metadata.ProviderUri);
        Assert.Null(metadata.ModelId);
    }

    [Fact]
    public void Constructor_Value_Roundtrips()
    {
        var uri = new Uri("https://example.com");
        AudioTranscriptionClientMetadata metadata = new("providerName", uri, "theModel");
        Assert.Equal("providerName", metadata.ProviderName);
        Assert.Same(uri, metadata.ProviderUri);
        Assert.Equal("theModel", metadata.ModelId);
    }
}
