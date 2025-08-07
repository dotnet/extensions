// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;
using OpenAI;
using OpenAI.Images;

#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields

namespace Microsoft.Extensions.AI;

/// <summary>Represents an <see cref="IImageClient"/> for an OpenAI <see cref="OpenAIClient"/> or <see cref="ImageClient"/>.</summary>
internal sealed class OpenAIImageClient : IImageClient
{
    /// <summary>Metadata about the client.</summary>
    private readonly ImageClientMetadata _metadata;

    /// <summary>The underlying <see cref="ImageClient" />.</summary>
    private readonly ImageClient _imageClient;

    /// <summary>Initializes a new instance of the <see cref="OpenAIImageClient"/> class for the specified <see cref="ImageClient"/>.</summary>
    /// <param name="imageClient">The underlying client.</param>
    /// <exception cref="ArgumentNullException"><paramref name="imageClient"/> is <see langword="null"/>.</exception>
    public OpenAIImageClient(ImageClient imageClient)
    {
        _ = Throw.IfNull(imageClient);

        _imageClient = imageClient;

        // https://github.com/openai/openai-dotnet/issues/215
        // The endpoint and model aren't currently exposed, so use reflection to get at them, temporarily. Once packages
        // implement the abstractions directly rather than providing adapters on top of the public APIs,
        // the package can provide such implementations separate from what's exposed in the public API.
        Uri providerUrl = typeof(ImageClient).GetField("_endpoint", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(imageClient) as Uri ?? OpenAIClientExtensions.DefaultOpenAIEndpoint;

        _metadata = new("openai", providerUrl, _imageClient.Model);
    }

    /// <inheritdoc />
    public async Task<ImageResponse> GenerateImagesAsync(ImageRequest request, ImageOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(request);

        string? prompt = request.Prompt;
        _ = Throw.IfNull(prompt);

        // If the request has original images, treat this as an edit operation
        if (request.OriginalImages != null)
        {
            return await EditImagesAsync(request.OriginalImages, prompt, options, cancellationToken).ConfigureAwait(false);
        }

        ImageGenerationOptions openAIOptions = ToOpenAIImageGenerationOptions(options);

        GeneratedImageCollection result = await _imageClient.GenerateImagesAsync(prompt, options?.Count ?? 1, openAIOptions, cancellationToken).ConfigureAwait(false);

        return ToImageResponse(result);
    }

    /// <inheritdoc />
    public async Task<ImageResponse> EditImagesAsync(
        IEnumerable<AIContent> originalImages, string prompt, ImageOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(originalImages);
        _ = Throw.IfNull(prompt);

        ImageEditOptions openAIOptions = ToOpenAIImageEditOptions(options);
        string? fileName = null;
        Stream? imageStream = null;

        foreach (AIContent originalImage in originalImages)
        {
            if (imageStream is not null)
            {
                // We only support a single image for editing.
                Throw.ArgumentException(
                    "Only a single image can be provided for editing.",
                    nameof(originalImages));
            }

            if (originalImage is DataContent dataContent)
            {
                imageStream = MemoryMarshal.TryGetArray(dataContent.Data, out var array) ?
                    new MemoryStream(array.Array!, array.Offset, array.Count) :
                    new MemoryStream(dataContent.Data.ToArray());
                fileName = dataContent.Name ?? dataContent.MediaType switch
                {
                    "image/png" => "image.png",
                    "image/jpeg" => "image.jpg",
                    "image/webp" => "image.webp",
                    _ => "image"
                };
            }
            else
            {
                // We might be able to handle UriContent by downloading the image, but need to plumb an HttpClient for that.
                // For now, we only support DataContent for image editing as OpenAI's API expects image data in a stream.
                Throw.ArgumentException(
                    "The original image must be a DataContent instance containing image data.",
                    nameof(originalImages));
            }
        }

        GeneratedImageCollection result = await _imageClient.GenerateImageEditsAsync(
            imageStream, fileName, prompt, options?.Count ?? 1, openAIOptions, cancellationToken).ConfigureAwait(false);

        return ToImageResponse(result);
    }

    /// <inheritdoc />
#pragma warning disable S1067 // Expressions should not be too complex
    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType is null ? throw new ArgumentNullException(nameof(serviceType)) :
        serviceKey is not null ? null :
        serviceType == typeof(ImageClientMetadata) ? _metadata :
        serviceType == typeof(ImageClient) ? _imageClient :
        serviceType.IsInstanceOfType(this) ? this :
        null;
#pragma warning restore S1067 // Expressions should not be too complex

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        // Nothing to dispose. Implementation required for the IImageClient interface.
    }

    /// <summary>
    /// Converts a <see cref="Size"/> to an OpenAI <see cref="GeneratedImageSize"/>.
    /// </summary>
    /// <param name="requestedSize">User's requested size.</param>
    /// <returns>Closest supported size.</returns>
    private static GeneratedImageSize? ToOpenAIImageSize(Size? requestedSize) =>
        requestedSize is null ? null : new GeneratedImageSize(requestedSize.Value.Width, requestedSize.Value.Height);

    /// <summary>Converts a <see cref="GeneratedImageCollection"/> to a <see cref="ImageResponse"/>.</summary>
    private static ImageResponse ToImageResponse(GeneratedImageCollection generatedImages)
    {
        string contentType = "image/png"; // Default content type for images

        // OpenAI doesn't expose the content type, so we need to read from the internal JSON representation.
        // https://github.com/openai/openai-dotnet/issues/561
        IDictionary<string, BinaryData>? additionalRawData = typeof(GeneratedImageCollection)
            .GetProperty("SerializedAdditionalRawData", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(generatedImages) as IDictionary<string, BinaryData>;

        if (additionalRawData?.TryGetValue("output_format", out var outputFormat) ?? false)
        {
            var stringJsonTypeInfo = (JsonTypeInfo<string>)AIJsonUtilities.DefaultOptions.GetTypeInfo(typeof(string));
            var outputFormatString = JsonSerializer.Deserialize(outputFormat, stringJsonTypeInfo);
            contentType = $"image/{outputFormatString}";
        }

        List<AIContent> contents = new();

        foreach (GeneratedImage image in generatedImages)
        {
            if (image.ImageBytes is not null)
            {
                contents.Add(new DataContent(image.ImageBytes.ToMemory(), contentType));
            }
            else if (image.ImageUri is not null)
            {
                contents.Add(new UriContent(image.ImageUri, contentType));
            }
            else
            {
                throw new InvalidOperationException("Generated image does not contain a valid URI or byte array.");
            }
        }

        return new ImageResponse(contents)
        {
            RawRepresentation = generatedImages
        };
    }

    /// <summary>Converts a <see cref="ImageOptions"/> to a <see cref="ImageGenerationOptions"/>.</summary>
    private ImageGenerationOptions ToOpenAIImageGenerationOptions(ImageOptions? options)
    {
        ImageGenerationOptions result = options?.RawRepresentationFactory?.Invoke(this) as ImageGenerationOptions ?? new();

        result.Background ??= options?.Background;

        result.OutputFileFormat ??= options?.MediaType switch
        {
            "image/png" => GeneratedImageFileFormat.Png,
            "image/jpeg" => GeneratedImageFileFormat.Jpeg,
            "image/webp" => GeneratedImageFileFormat.Webp,
            _ => null,
        };

        result.ResponseFormat ??= options?.ResponseFormat switch
        {
            ImageResponseFormat.Uri => GeneratedImageFormat.Uri,
            ImageResponseFormat.Data => GeneratedImageFormat.Bytes,

            // ImageResponseFormat.Hosted not supported by ImageClient, however other OpenAI API support file IDs.
            _ => null
        };

        result.Size ??= ToOpenAIImageSize(options?.ImageSize);

        result.Style ??= options?.Style;

        return result;
    }

    /// <summary>Converts a <see cref="ImageOptions"/> to a <see cref="ImageEditOptions"/>.</summary>
    private ImageEditOptions ToOpenAIImageEditOptions(ImageOptions? options)
    {
        ImageEditOptions result = options?.RawRepresentationFactory?.Invoke(this) as ImageEditOptions ?? new();

        result.ResponseFormat ??= options?.ResponseFormat switch
        {
            ImageResponseFormat.Uri => GeneratedImageFormat.Uri,
            ImageResponseFormat.Data => GeneratedImageFormat.Bytes,

            // ImageResponseFormat.Hosted not supported by ImageClient, however other OpenAI API support file IDs.
            _ => null
        };

        result.Size ??= ToOpenAIImageSize(options?.ImageSize);

        return result;
    }
}
