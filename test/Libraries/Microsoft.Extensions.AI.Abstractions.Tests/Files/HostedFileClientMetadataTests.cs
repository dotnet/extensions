// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable MEAI001

using System;
using Xunit;

namespace Microsoft.Extensions.AI;

public class HostedFileClientMetadataTests
{
    [Fact]
    public void DefaultConstructor_PropertiesAreNull()
    {
        var metadata = new HostedFileClientMetadata();
        Assert.Null(metadata.ProviderName);
        Assert.Null(metadata.ProviderUri);
    }

    [Fact]
    public void Constructor_WithBothValues_Roundtrips()
    {
        var uri = new Uri("https://api.openai.com");
        var metadata = new HostedFileClientMetadata("openai", uri);
        Assert.Equal("openai", metadata.ProviderName);
        Assert.Same(uri, metadata.ProviderUri);
    }

    [Fact]
    public void Constructor_WithOnlyProviderName()
    {
        var metadata = new HostedFileClientMetadata(providerName: "anthropic");
        Assert.Equal("anthropic", metadata.ProviderName);
        Assert.Null(metadata.ProviderUri);
    }

    [Fact]
    public void Constructor_WithOnlyProviderUri()
    {
        var uri = new Uri("https://api.example.com");
        var metadata = new HostedFileClientMetadata(providerUri: uri);
        Assert.Null(metadata.ProviderName);
        Assert.Same(uri, metadata.ProviderUri);
    }
}
