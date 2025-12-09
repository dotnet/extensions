// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extension methods for <see cref="IImageGenerator"/>.</summary>
[Experimental(DiagnosticIds.Experiments.ImageGeneration, UrlFormat = DiagnosticIds.UrlFormat, Message = DiagnosticIds.Experiments.ImageGenerationMessage)]
public static class ImageGeneratorExtensions
{
    private static readonly Dictionary<string, string> _extensionToMimeType = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".webp"] = "image/webp",
        [".gif"] = "image/gif",
        [".bmp"] = "image/bmp",
        [".tiff"] = "image/tiff",
        [".tif"] = "image/tiff",
    };

    /// <summary>Asks the <see cref="IImageGenerator"/> for an object of type <typeparamref name="TService"/>.</summary>
    /// <typeparam name="TService">The type of the object to be retrieved.</typeparam>
    /// <param name="generator">The generator.</param>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object, otherwise <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="generator"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The purpose of this method is to allow for the retrieval of strongly typed services that may be provided by the <see cref="IImageGenerator"/>,
    /// including itself or any services it might be wrapping.
    /// </remarks>
    public static TService? GetService<TService>(this IImageGenerator generator, object? serviceKey = null)
    {
        _ = Throw.IfNull(generator);

        return generator.GetService(typeof(TService), serviceKey) is TService service ? service : default;
    }

    /// <summary>
    /// Asks the <see cref="IImageGenerator"/> for an object of the specified type <paramref name="serviceType"/>
    /// and throws an exception if one isn't available.
    /// </summary>
    /// <param name="generator">The generator.</param>
    /// <param name="serviceType">The type of object being requested.</param>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="generator"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="serviceType"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No service of the requested type for the specified key is available.</exception>
    /// <remarks>
    /// The purpose of this method is to allow for the retrieval of services that are required to be provided by the <see cref="IImageGenerator"/>,
    /// including itself or any services it might be wrapping.
    /// </remarks>
    public static object GetRequiredService(this IImageGenerator generator, Type serviceType, object? serviceKey = null)
    {
        _ = Throw.IfNull(generator);
        _ = Throw.IfNull(serviceType);

        return
            generator.GetService(serviceType, serviceKey) ??
            throw Throw.CreateMissingServiceException(serviceType, serviceKey);
    }

    /// <summary>
    /// Asks the <see cref="IImageGenerator"/> for an object of type <typeparamref name="TService"/>
    /// and throws an exception if one isn't available.
    /// </summary>
    /// <typeparam name="TService">The type of the object to be retrieved.</typeparam>
    /// <param name="generator">The generator.</param>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="generator"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No service of the requested type for the specified key is available.</exception>
    /// <remarks>
    /// The purpose of this method is to allow for the retrieval of strongly typed services that are required to be provided by the <see cref="IImageGenerator"/>,
    /// including itself or any services it might be wrapping.
    /// </remarks>
    public static TService GetRequiredService<TService>(this IImageGenerator generator, object? serviceKey = null)
    {
        _ = Throw.IfNull(generator);

        if (generator.GetService(typeof(TService), serviceKey) is not TService service)
        {
            throw Throw.CreateMissingServiceException(typeof(TService), serviceKey);
        }

        return service;
    }

    /// <summary>
    /// Generates images based on a text prompt.
    /// </summary>
    /// <param name="generator">The image generator.</param>
    /// <param name="prompt">The prompt to guide the image generation.</param>
    /// <param name="options">The image generation options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="generator"/> or <paramref name="prompt"/> is <see langword="null"/>.</exception>
    /// <returns>The images generated by the generator.</returns>
    public static Task<ImageGenerationResponse> GenerateImagesAsync(
        this IImageGenerator generator,
        string prompt,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(generator);
        _ = Throw.IfNull(prompt);

        return generator.GenerateAsync(new ImageGenerationRequest(prompt), options, cancellationToken);
    }

    /// <summary>
    /// Edits images based on original images and a text prompt.
    /// </summary>
    /// <param name="generator">The image generator.</param>
    /// <param name="originalImages">The images to base edits on.</param>
    /// <param name="prompt">The prompt to guide the image editing.</param>
    /// <param name="options">The image generation options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="generator"/>, <paramref name="originalImages"/>, or <paramref name="prompt"/> is <see langword="null"/>.</exception>
    /// <returns>The images generated by the generator.</returns>
    public static Task<ImageGenerationResponse> EditImagesAsync(
        this IImageGenerator generator,
        IEnumerable<AIContent> originalImages,
        string prompt,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(generator);
        _ = Throw.IfNull(originalImages);
        _ = Throw.IfNull(prompt);

        return generator.GenerateAsync(new ImageGenerationRequest(prompt, originalImages), options, cancellationToken);
    }

    /// <summary>
    /// Edits a single image based on the original image and the specified prompt.
    /// </summary>
    /// <param name="generator">The image generator.</param>
    /// <param name="originalImage">The single image to base edits on.</param>
    /// <param name="prompt">The prompt to guide the image generation.</param>
    /// <param name="options">The image generation options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="generator"/>, <paramref name="originalImage"/>, or <paramref name="prompt"/> is <see langword="null"/>.</exception>
    /// <returns>The images generated by the generator.</returns>
    public static Task<ImageGenerationResponse> EditImageAsync(
        this IImageGenerator generator,
        DataContent originalImage,
        string prompt,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(generator);
        _ = Throw.IfNull(originalImage);
        _ = Throw.IfNull(prompt);

        return generator.GenerateAsync(new ImageGenerationRequest(prompt, [originalImage]), options, cancellationToken);
    }

    /// <summary>
    /// Edits a single image based on a byte array and the specified prompt.
    /// </summary>
    /// <param name="generator">The image generator.</param>
    /// <param name="originalImageData">The byte array containing the image data to base edits on.</param>
    /// <param name="fileName">The filename for the image data.</param>
    /// <param name="prompt">The prompt to guide the image generation.</param>
    /// <param name="options">The image generation options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="generator"/>, <paramref name="fileName"/>, or <paramref name="prompt"/> is <see langword="null"/>.
    /// </exception>
    /// <returns>The images generated by the generator.</returns>
    public static Task<ImageGenerationResponse> EditImageAsync(
        this IImageGenerator generator,
        ReadOnlyMemory<byte> originalImageData,
        string fileName,
        string prompt,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(generator);
        _ = Throw.IfNull(fileName);
        _ = Throw.IfNull(prompt);

        // Infer media type from file extension
        string mediaType = GetMediaTypeFromFileName(fileName);

        var dataContent = new DataContent(originalImageData, mediaType) { Name = fileName };
        return generator.GenerateAsync(new ImageGenerationRequest(prompt, [dataContent]), options, cancellationToken);
    }

    /// <summary>
    /// Gets the media type based on the file extension.
    /// </summary>
    /// <param name="fileName">The filename to extract the media type from.</param>
    /// <returns>The inferred media type.</returns>
    private static string GetMediaTypeFromFileName(string fileName)
    {
        string extension = Path.GetExtension(fileName);

        if (_extensionToMimeType.TryGetValue(extension, out string? mediaType))
        {
            return mediaType;
        }

        return "image/png"; // Default to PNG if unknown extension
    }
}
