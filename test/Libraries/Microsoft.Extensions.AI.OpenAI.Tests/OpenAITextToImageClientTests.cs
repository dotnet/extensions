// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
using OpenAI;
using OpenAI.Images;
using Xunit;

namespace Microsoft.Extensions.AI;

public class OpenAIImageClientTests
{
    [Fact]
    public void AsIImageClient_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("imageClient", () => ((ImageClient)null!).AsIImageClient());
    }

    [Fact]
    public void AsIImageClient_OpenAIClient_ProducesExpectedMetadata()
    {
        Uri endpoint = new("http://localhost/some/endpoint");
        string model = "dall-e-3";

        var client = new OpenAIClient(new ApiKeyCredential("key"), new OpenAIClientOptions { Endpoint = endpoint });

        IImageClient imageClient = client.GetImageClient(model).AsIImageClient();
        var metadata = imageClient.GetService<ImageClientMetadata>();
        Assert.Equal(endpoint, metadata?.ProviderUri);
        Assert.Equal(model, metadata?.DefaultModelId);
    }

    [Fact]
    public void GetService_ReturnsExpectedServices()
    {
        var client = new OpenAIClient(new ApiKeyCredential("key"));
        IImageClient imageClient = client.GetImageClient("dall-e-3").AsIImageClient();

        Assert.Same(imageClient, imageClient.GetService<IImageClient>());
        Assert.Same(imageClient, imageClient.GetService<object>());
        Assert.NotNull(imageClient.GetService<ImageClientMetadata>());
        Assert.NotNull(imageClient.GetService<ImageClient>());
    }
}
