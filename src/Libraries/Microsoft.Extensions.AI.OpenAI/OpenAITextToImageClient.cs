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

        GeneratedImageCollection result = await imageClient.GenerateImagesAsync(prompt, options.Count ?? 1, openAIOptions, cancellationToken).ConfigureAwait(false);

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
            originalImage, originalImageFileName, prompt, options.Count ?? 1, openAIOptions, cancellationToken).ConfigureAwait(false);

        return ToTextToImageResponse(result, options);
    }

    /// <inheritdoc />
#pragma warning disable S1067 // Expressions should not be too complex
    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType is null ? throw new ArgumentNullException(nameof(serviceType)) :
        serviceKey is not null ? null :
        serviceType == typeof(TextToImageClientMetadata) ? _metadata :
        serviceType == typeof(ImageClient) ? _imageClient :
        serviceType == typeof(OpenAIClient) ? _openAIClient :
        serviceType.IsInstanceOfType(this) ? this :
        null;
#pragma warning restore S1067 // Expressions should not be too complex

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        // Nothing to dispose. Implementation required for the ITextToImageClient interface.
    }

    /// <summary>Converts a <see cref="GeneratedImageCollection"/> to a <see cref="TextToImageResponse"/>.</summary>
    private static TextToImageResponse ToTextToImageResponse(GeneratedImageCollection generatedImages, TextToImageOptions options)
    {
        List<AIContent> contents = new();

        foreach (GeneratedImage image in generatedImages)
        {
            if (image.ImageBytes is not null)
            {
                contents.Add(new DataContent(image.ImageBytes.ToArray(), "image/png"));
            }
            else if (image.ImageUri is not null) 
            {
                contents.Add(new UriContent(image.ImageUri, "image/png"));
            }
            else
            {
                throw new InvalidOperationException("Generated image does not contain a valid URI or byte array.");
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
            Size = ToOpenAIImageSize(options.ImageSize, options.ModelId)
        };

        if (options.ContentType is not null)
        {
            result.ResponseFormat = options.ContentType == TextToImageContentType.Uri
                ? GeneratedImageFormat.Uri
                : GeneratedImageFormat.Bytes;
        }

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
            Size = ToOpenAIImageSize(options.ImageSize, options.ModelId),
        };

        if (options.ContentType is not null)
        {
            result.ResponseFormat = options.ContentType == TextToImageContentType.Uri
                ? GeneratedImageFormat.Uri
                : GeneratedImageFormat.Bytes;
        }

        return result;
    }

    /// <summary>
    /// Converts a <see cref="Size"/> to an OpenAI <see cref="GeneratedImageSize"/>.
    /// </summary>
    /// <param name="requestedSize">User's requested size.</param>
    /// <param name="modelId">Model to consider for supported sizes.</param>
    /// <returns>Closest supported size.</returns>
    private GeneratedImageSize? ToOpenAIImageSize(Size? requestedSize, string? modelId = null)
    {
        modelId ??= _defaultModelId;

        // from https://platform.openai.com/docs/api-reference/images
        // The size of the generated images.
        // Must be one of 1024x1024, 1536x1024 (landscape), 1024x1536 (portrait), or auto (default value) for gpt-image-1,
        // one of 256x256, 512x512, or 1024x1024 for dall-e-2,
        // and one of 1024x1024, 1792x1024, or 1024x1792 for dall-e-3.
#pragma warning disable S109 // Magic numbers should not be used
        return modelId switch
        {
            "gpt-image-1" => GetClosestImageSize(
            [
                (GeneratedImageSize.W1024xH1024, 1024 * 1024),
                (GeneratedImageSize.W1536xH1024, 1536 * 1024),
                (GeneratedImageSize.W1024xH1536, 1024 * 1536)
            ]),
            "dall-e-2" => GetClosestImageSize(
            [
                (GeneratedImageSize.W256xH256, 256 * 256),
                (GeneratedImageSize.W512xH512, 512 * 512),
                (GeneratedImageSize.W1024xH1024, 1024 * 1024)
            ]),
            "dall-e-3" => GetClosestImageSize(
            [
                (GeneratedImageSize.W1024xH1024, 1024 * 1024),
                (GeneratedImageSize.W1792xH1024, 1792 * 1024),
                (GeneratedImageSize.W1024xH1792, 1024 * 1792)
            ]),
            _ => null // No default size for other models
        };
#pragma warning restore S109 // Magic numbers should not be used

        GeneratedImageSize? GetClosestImageSize(ReadOnlySpan<(GeneratedImageSize size, double area)> supportedSizes)
        {
            if (requestedSize is null || requestedSize.Value.IsEmpty)
            {
                // If no size is requested, return null to use the default size for the model.
                return null;
            }

            double requestedArea = requestedSize.Value.Width * requestedSize.Value.Height;

            GeneratedImageSize? closestSize = null;
            double closestArea = double.MaxValue;

            foreach (var supportedSize in supportedSizes)
            {
                double area = Math.Abs(supportedSize.area - requestedArea);
                if (area < closestArea)
                {
                    closestArea = area;
                    closestSize = supportedSize.size;
                }
            }

            return closestSize;
        }
    }
}
