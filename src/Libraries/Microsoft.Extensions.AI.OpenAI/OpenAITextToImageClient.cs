// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;
using OpenAI;
using OpenAI.Images;

#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields

namespace Microsoft.Extensions.AI;

/// <summary>Represents an <see cref="ITextToImageClient"/> for an OpenAI <see cref="OpenAIClient"/> or <see cref="ImageClient"/>.</summary>
internal sealed class OpenAITextToImageClient : ITextToImageClient
{
    /// <summary>Metadata about the client.</summary>
    private readonly TextToImageClientMetadata _metadata;

    /// <summary>The underlying <see cref="ImageClient" />.</summary>
    private readonly ImageClient _imageClient;

    /// <summary>The underlying <see cref="OpenAIClient" />.</summary>
    private readonly OpenAIClient? _openAIClient;

    /// <summary>The default model to use for image generation.</summary>
    private readonly string? _defaultModelId;

    /// <summary>Initializes a new instance of the <see cref="OpenAITextToImageClient"/> class for the specified <see cref="ImageClient"/>.</summary>
    /// <param name="imageClient">The underlying client.</param>
    /// <exception cref="ArgumentNullException"><paramref name="imageClient"/> is <see langword="null"/>.</exception>
    public OpenAITextToImageClient(ImageClient imageClient)
    {
        _ = Throw.IfNull(imageClient);

        _imageClient = imageClient;

        // https://github.com/openai/openai-dotnet/issues/215
        // The endpoint and model aren't currently exposed, so use reflection to get at them, temporarily. Once packages
        // implement the abstractions directly rather than providing adapters on top of the public APIs,
        // the package can provide such implementations separate from what's exposed in the public API.
        Uri providerUrl = typeof(ImageClient).GetField("_endpoint", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(imageClient) as Uri ?? OpenAIClientExtensions.DefaultOpenAIEndpoint;

        _defaultModelId = typeof(ImageClient).GetField("_model", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(imageClient) as string;

        _metadata = new("openai", providerUrl, _defaultModelId);
    }

    /// <summary>Initializes a new instance of the <see cref="OpenAITextToImageClient"/> class for the specified <see cref="OpenAIClient"/> and model.  
    /// Use this constructor if you wish you support changing the model with <see cref="TextToImageOptions.ModelId"/>.</summary>
    /// <param name="openAIClient">The underlying OpenAI client.</param>
    /// <param name="model">The default model to use for image generation.</param>  
    public OpenAITextToImageClient(OpenAIClient openAIClient, string model)
        : this(Throw.IfNull(openAIClient).GetImageClient(model))
    {
        _openAIClient = openAIClient;
    }

    /// <inheritdoc />
    public async Task<TextToImageResponse> GenerateImagesAsync(string prompt, TextToImageOptions? options, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(prompt);
        _ = Throw.IfNull(options);

        ImageGenerationOptions openAIOptions = ToOpenAIImageGenerationOptions(options);
        ImageClient imageClient = GetImageClient(options);

        GeneratedImageCollection result = await imageClient.GenerateImagesAsync(prompt, options.Count, openAIOptions, cancellationToken).ConfigureAwait(false);

        return ToTextToImageResponse(result, options);
    }

    /// <inheritdoc />
    public async Task<TextToImageResponse> GenerateEditImageAsync(
        Stream originalImage, string originalImageFileName, string prompt, TextToImageOptions? options, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(originalImage);
        _ = Throw.IfNull(originalImageFileName);
        _ = Throw.IfNull(prompt);
        _ = Throw.IfNull(options);

        ImageEditOptions openAIOptions = ToOpenAIImageEditOptions(options);
        ImageClient imageClient = GetImageClient(options);

        GeneratedImageCollection result = await imageClient.GenerateImageEditsAsync(
            originalImage, originalImageFileName, prompt, options.Count, openAIOptions, cancellationToken).ConfigureAwait(false);

        return ToTextToImageResponse(result, options);
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        _ = Throw.IfNull(serviceType);

        if (serviceType.IsInstanceOfType(this))
        {
            return this;
        }

        if (serviceType == typeof(TextToImageClientMetadata))
        {
            return _metadata;
        }

        if (serviceType.IsInstanceOfType(_imageClient))
        {
            return _imageClient;
        }

        return null;
    }

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        // Nothing to dispose. Implementation required for the ITextToImageClient interface.
    }

    /// <summary>Converts a <see cref="Size"/> to a <see cref="GeneratedImageSize"/>.</summary>
    private static GeneratedImageSize? ToOpenAIImageSize(Size size)
    {
        return new GeneratedImageSize(size.Width, size.Height);
    }

    /// <summary>Converts a <see cref="GeneratedImageCollection"/> to a <see cref="TextToImageResponse"/>.</summary>
    private static TextToImageResponse ToTextToImageResponse(GeneratedImageCollection generatedImages, TextToImageOptions options)
    {
        List<AIContent> contents = new();

        foreach (GeneratedImage image in generatedImages)
        {
            if (options.ContentType == TextToImageContentType.Uri && image.ImageUri is not null)
            {
                contents.Add(new UriContent(image.ImageUri, "image/png"));
            }
            else if (options.ContentType == TextToImageContentType.Data && image.ImageBytes is not null)
            {
                contents.Add(new DataContent(image.ImageBytes.ToArray(), "image/png"));
            }
        }

        return new TextToImageResponse(contents)
        {
            RawRepresentation = generatedImages
        };
    }

    private ImageClient GetImageClient(TextToImageOptions options)
    {
        if (options.ModelId is null || options.ModelId == _defaultModelId)
        {
            // If default model is requested
            return _imageClient;
        }

        // If a specific model is requested, get the image client for that model
        return _openAIClient?.GetImageClient(options.ModelId) ??
            throw new InvalidOperationException($"Cannot create an ImageClient for {options.ModelId}.  Please ensure {nameof(OpenAITextToImageClient)} is initialized with an {nameof(OpenAIClient)}.");
    }

    /// <summary>Converts a <see cref="TextToImageOptions"/> to a <see cref="ImageGenerationOptions"/>.</summary>
    private ImageGenerationOptions ToOpenAIImageGenerationOptions(TextToImageOptions options)
    {
        if (options.RawRepresentationFactory?.Invoke(this) is ImageGenerationOptions result)
        {
            return result;
        }

        result = new ImageGenerationOptions
        {
            Size = ToOpenAIImageSize(options.ImageSize),
            ResponseFormat = options.ContentType == TextToImageContentType.Uri ? GeneratedImageFormat.Uri : GeneratedImageFormat.Bytes,
            Quality = GeneratedImageQuality.Standard // Default quality
        };

        return result;
    }

    /// <summary>Converts a <see cref="TextToImageOptions"/> to a <see cref="ImageEditOptions"/>.</summary>
    private ImageEditOptions ToOpenAIImageEditOptions(TextToImageOptions options)
    {
        if (options.RawRepresentationFactory?.Invoke(this) is ImageEditOptions result)
        {
            return result;
        }

        result = new ImageEditOptions
        {
            Size = ToOpenAIImageSize(options.ImageSize),
            ResponseFormat = options.ContentType == TextToImageContentType.Uri ? GeneratedImageFormat.Uri : GeneratedImageFormat.Bytes
        };

        return result;
    }
}
