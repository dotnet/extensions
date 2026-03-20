// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable OPENAI001 // Experimental OpenAI APIs

using System;
using System.ClientModel;
using OpenAI;
using OpenAI.Videos;
using Xunit;

namespace Microsoft.Extensions.AI;

public class OpenAIVideoGeneratorTests
{
    [Fact]
    public void AsIVideoGenerator_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("videoClient", () => ((VideoClient)null!).AsIVideoGenerator());
    }

    [Fact]
    public void AsIVideoGenerator_OpenAIClient_ProducesExpectedMetadata()
    {
        Uri endpoint = new("http://localhost/some/endpoint");
        string model = "sora";

        var client = new OpenAIClient(new ApiKeyCredential("key"), new OpenAIClientOptions { Endpoint = endpoint });

        IVideoGenerator videoGenerator = client.GetVideoClient().AsIVideoGenerator(model);
        var metadata = videoGenerator.GetService<VideoGeneratorMetadata>();
        Assert.Equal(endpoint, metadata?.ProviderUri);
        Assert.Equal(model, metadata?.DefaultModelId);
    }

    [Fact]
    public void GetService_ReturnsExpectedServices()
    {
        var client = new OpenAIClient(new ApiKeyCredential("key"));
        IVideoGenerator videoGenerator = client.GetVideoClient().AsIVideoGenerator("sora");

        Assert.Same(videoGenerator, videoGenerator.GetService<IVideoGenerator>());
        Assert.Same(videoGenerator, videoGenerator.GetService<object>());
        Assert.NotNull(videoGenerator.GetService<VideoGeneratorMetadata>());
        Assert.NotNull(videoGenerator.GetService<VideoClient>());
    }
}
