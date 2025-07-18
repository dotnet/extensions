// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
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
        Assert.Throws<ArgumentNullException>("openAIClient", () => ((OpenAIClient)null!).AsITextToImageClient("dall-e-3"));
        Assert.Throws<ArgumentNullException>("model", () => new OpenAIClient(new ApiKeyCredential("key")).AsITextToImageClient(null!));
    }

    [Fact]
    public void AsITextToImageClient_OpenAIClient_ProducesExpectedMetadata()
    {
        Uri endpoint = new("http://localhost/some/endpoint");
        string model = "dall-e-3";

        var client = new OpenAIClient(new ApiKeyCredential("key"), new OpenAIClientOptions { Endpoint = endpoint });

        ITextToImageClient textToImageClient = client.AsITextToImageClient(model);
        var metadata = textToImageClient.GetService<TextToImageClientMetadata>();
        Assert.Equal(endpoint, metadata?.ProviderUri);
        Assert.Equal(model, metadata?.DefaultModelId);

        textToImageClient = client.GetImageClient(model).AsITextToImageClient();
        metadata = textToImageClient.GetService<TextToImageClientMetadata>();
        Assert.Equal(endpoint, metadata?.ProviderUri);
        Assert.Equal(model, metadata?.DefaultModelId);
    }

    [Fact]
    public void GetService_ReturnsExpectedServices()
    {
        var client = new OpenAIClient(new ApiKeyCredential("key"));
        ITextToImageClient textToImageClient = client.AsITextToImageClient("dall-e-3");

        Assert.Same(textToImageClient, textToImageClient.GetService<ITextToImageClient>());
        Assert.NotNull(textToImageClient.GetService<TextToImageClientMetadata>());
        Assert.NotNull(textToImageClient.GetService<ImageClient>());
        Assert.Null(textToImageClient.GetService<object>());
    }

    [Fact]
    public void TextToImageOptions_DefaultValues()
    {
        var options = new TextToImageOptions();
        
        Assert.Equal(TextToImageContentType.Uri, options.ContentType);
        Assert.Equal(1, options.Count);
        Assert.Equal(new Size(512, 512), options.ImageSize);
        Assert.Null(options.GuidanceScale);
        Assert.Null(options.ModelId);
        Assert.Null(options.NegativePrompt);
        Assert.Equal(0, options.Steps);
        Assert.Null(options.RawRepresentationFactory);
    }

    [Fact]
    public void TextToImageOptions_Clone()
    {
        var original = new TextToImageOptions
        {
            ContentType = TextToImageContentType.Data,
            Count = 3,
            GuidanceScale = 7.5f,
            ImageSize = new Size(1024, 1024),
            ModelId = "dall-e-3",
            NegativePrompt = "blurry",
            Steps = 50
        };

        var cloned = original.Clone();

        Assert.Equal(original.ContentType, cloned.ContentType);
        Assert.Equal(original.Count, cloned.Count);
        Assert.Equal(original.GuidanceScale, cloned.GuidanceScale);
        Assert.Equal(original.ImageSize, cloned.ImageSize);
        Assert.Equal(original.ModelId, cloned.ModelId);
        Assert.Equal(original.NegativePrompt, cloned.NegativePrompt);
        Assert.Equal(original.Steps, cloned.Steps);
    }

    [Fact]
    public void TextToImageResponse_DefaultConstructor()
    {
        var response = new TextToImageResponse();
        Assert.NotNull(response.Contents);
        Assert.Empty(response.Contents);
        Assert.Null(response.RawRepresentation);
    }

    [Fact]
    public void TextToImageResponse_WithContents()
    {
        var contents = new[] { new UriContent("https://example.com/image.png", "image/png") };
        var response = new TextToImageResponse(contents);
        
        Assert.Same(contents, response.Contents);
        Assert.Null(response.RawRepresentation);
    }

    [Fact]
    public void TextToImageClientMetadata_DefaultValues()
    {
        var metadata = new TextToImageClientMetadata();
        
        Assert.Null(metadata.ProviderName);
        Assert.Null(metadata.ProviderUri);
        Assert.Null(metadata.DefaultModelId);
    }

    [Fact]
    public void TextToImageClientMetadata_WithValues()
    {
        var uri = new Uri("https://api.openai.com/v1");
        var metadata = new TextToImageClientMetadata("openai", uri, "dall-e-3");
        
        Assert.Equal("openai", metadata.ProviderName);
        Assert.Equal(uri, metadata.ProviderUri);
        Assert.Equal("dall-e-3", metadata.DefaultModelId);
    }
}
