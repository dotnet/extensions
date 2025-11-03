// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
using OpenAI;
using OpenAI.Images;
using Xunit;

namespace Microsoft.Extensions.AI;

public class OpenAIImageGeneratorTests
{
    [Fact]
    public void AsIImageGenerator_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("imageClient", () => ((ImageClient)null!).AsIImageGenerator());
    }

    [Fact]
    public void AsIImageGenerator_OpenAIClient_ProducesExpectedMetadata()
    {
        Uri endpoint = new("http://localhost/some/endpoint");
        string model = "dall-e-3";

        var client = new OpenAIClient(new ApiKeyCredential("key"), new OpenAIClientOptions { Endpoint = endpoint });

        IImageGenerator imageClient = client.GetImageClient(model).AsIImageGenerator();
        var metadata = imageClient.GetService<ImageGeneratorMetadata>();
        Assert.Equal(endpoint, metadata?.ProviderUri);
        Assert.Equal(model, metadata?.DefaultModelId);
    }

    [Fact]
    public void GetService_ReturnsExpectedServices()
    {
        var client = new OpenAIClient(new ApiKeyCredential("key"));
        IImageGenerator imageClient = client.GetImageClient("dall-e-3").AsIImageGenerator();

        Assert.Same(imageClient, imageClient.GetService<IImageGenerator>());
        Assert.Same(imageClient, imageClient.GetService<object>());
        Assert.NotNull(imageClient.GetService<ImageGeneratorMetadata>());
        Assert.NotNull(imageClient.GetService<ImageClient>());
    }
}
