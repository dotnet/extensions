// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
using OpenAI;
using OpenAI.Images;
using Xunit;

namespace Microsoft.Extensions.AI;

public class OpenAITextToImageClientTests
{
    [Fact]
    public void AsITextToImageClient_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("imageClient", () => ((ImageClient)null!).AsITextToImageClient());
    }

    [Fact]
    public void AsITextToImageClient_OpenAIClient_ProducesExpectedMetadata()
    {
        Uri endpoint = new("http://localhost/some/endpoint");
        string model = "dall-e-3";

        var client = new OpenAIClient(new ApiKeyCredential("key"), new OpenAIClientOptions { Endpoint = endpoint });

        ITextToImageClient textToImageClient = client.GetImageClient(model).AsITextToImageClient();
        var metadata = textToImageClient.GetService<TextToImageClientMetadata>();
        Assert.Equal(endpoint, metadata?.ProviderUri);
        Assert.Equal(model, metadata?.DefaultModelId);
    }

    [Fact]
    public void GetService_ReturnsExpectedServices()
    {
        var client = new OpenAIClient(new ApiKeyCredential("key"));
        ITextToImageClient textToImageClient = client.GetImageClient("dall-e-3").AsITextToImageClient();

        Assert.Same(textToImageClient, textToImageClient.GetService<ITextToImageClient>());
        Assert.Same(textToImageClient, textToImageClient.GetService<object>());
        Assert.NotNull(textToImageClient.GetService<TextToImageClientMetadata>());
        Assert.NotNull(textToImageClient.GetService<ImageClient>());
    }
}
