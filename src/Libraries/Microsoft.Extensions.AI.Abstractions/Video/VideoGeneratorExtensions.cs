// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extension methods for <see cref="IVideoGenerator"/>.</summary>
[Experimental(DiagnosticIds.Experiments.AIVideoGeneration, UrlFormat = DiagnosticIds.UrlFormat)]
public static class VideoGeneratorExtensions
{
    /// <summary>Asks the <see cref="IVideoGenerator"/> for an object of type <typeparamref name="TService"/>.</summary>
    /// <typeparam name="TService">The type of the object to be retrieved.</typeparam>
    /// <param name="generator">The generator.</param>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object, otherwise <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="generator"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The purpose of this method is to allow for the retrieval of strongly typed services that may be provided by the <see cref="IVideoGenerator"/>,
    /// including itself or any services it might be wrapping.
    /// </remarks>
    public static TService? GetService<TService>(this IVideoGenerator generator, object? serviceKey = null)
    {
        _ = Throw.IfNull(generator);

        return generator.GetService(typeof(TService), serviceKey) is TService service ? service : default;
    }

    /// <summary>
    /// Asks the <see cref="IVideoGenerator"/> for an object of the specified type <paramref name="serviceType"/>
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
    /// The purpose of this method is to allow for the retrieval of services that are required to be provided by the <see cref="IVideoGenerator"/>,
    /// including itself or any services it might be wrapping.
    /// </remarks>
    public static object GetRequiredService(this IVideoGenerator generator, Type serviceType, object? serviceKey = null)
    {
        _ = Throw.IfNull(generator);
        _ = Throw.IfNull(serviceType);

        return
            generator.GetService(serviceType, serviceKey) ??
            throw Throw.CreateMissingServiceException(serviceType, serviceKey);
    }

    /// <summary>
    /// Asks the <see cref="IVideoGenerator"/> for an object of type <typeparamref name="TService"/>
    /// and throws an exception if one isn't available.
    /// </summary>
    /// <typeparam name="TService">The type of the object to be retrieved.</typeparam>
    /// <param name="generator">The generator.</param>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="generator"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No service of the requested type for the specified key is available.</exception>
    /// <remarks>
    /// The purpose of this method is to allow for the retrieval of strongly typed services that are required to be provided by the <see cref="IVideoGenerator"/>,
    /// including itself or any services it might be wrapping.
    /// </remarks>
    public static TService GetRequiredService<TService>(this IVideoGenerator generator, object? serviceKey = null)
    {
        _ = Throw.IfNull(generator);

        if (generator.GetService(typeof(TService), serviceKey) is not TService service)
        {
            throw Throw.CreateMissingServiceException(typeof(TService), serviceKey);
        }

        return service;
    }

    /// <summary>
    /// Generates videos based on a text prompt.
    /// </summary>
    /// <param name="generator">The video generator.</param>
    /// <param name="prompt">The prompt to guide the video generation.</param>
    /// <param name="options">The video generation options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="generator"/> or <paramref name="prompt"/> is <see langword="null"/>.</exception>
    /// <returns>A <see cref="VideoGenerationOperation"/> representing the submitted video generation job.</returns>
    public static Task<VideoGenerationOperation> GenerateVideoAsync(
        this IVideoGenerator generator,
        string prompt,
        VideoGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(generator);
        _ = Throw.IfNull(prompt);

        return generator.GenerateAsync(new VideoGenerationRequest { Prompt = prompt }, options, cancellationToken);
    }

    /// <summary>
    /// Submits an edit request for existing video content using the specified prompt.
    /// </summary>
    /// <param name="generator">The video generator.</param>
    /// <param name="sourceVideo">The source video content to edit.</param>
    /// <param name="prompt">The prompt to guide the video editing.</param>
    /// <param name="options">The video generation options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="generator"/>, <paramref name="sourceVideo"/>, or <paramref name="prompt"/> is <see langword="null"/>.</exception>
    /// <returns>A <see cref="VideoGenerationOperation"/> representing the submitted video generation job.</returns>
    public static Task<VideoGenerationOperation> EditVideoAsync(
        this IVideoGenerator generator,
        AIContent sourceVideo,
        string prompt,
        VideoGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(generator);
        _ = Throw.IfNull(sourceVideo);
        _ = Throw.IfNull(prompt);

        return generator.GenerateAsync(
            new VideoGenerationRequest { Prompt = prompt, SourceVideo = sourceVideo, OperationKind = VideoOperationKind.Edit },
            options, cancellationToken);
    }

    /// <summary>
    /// Submits an edit request for a single video using the specified prompt.
    /// </summary>
    /// <param name="generator">The video generator.</param>
    /// <param name="sourceVideo">The single video to use as input.</param>
    /// <param name="prompt">The prompt to guide the video editing.</param>
    /// <param name="options">The video generation options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="generator"/>, <paramref name="sourceVideo"/>, or <paramref name="prompt"/> is <see langword="null"/>.</exception>
    /// <returns>A <see cref="VideoGenerationOperation"/> representing the submitted video generation job.</returns>
    public static Task<VideoGenerationOperation> EditVideoAsync(
        this IVideoGenerator generator,
        DataContent sourceVideo,
        string prompt,
        VideoGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(generator);
        _ = Throw.IfNull(sourceVideo);
        _ = Throw.IfNull(prompt);

        return generator.GenerateAsync(
            new VideoGenerationRequest { Prompt = prompt, SourceVideo = sourceVideo, OperationKind = VideoOperationKind.Edit },
            options, cancellationToken);
    }

    /// <summary>
    /// Submits an edit request for video data provided as a byte array.
    /// </summary>
    /// <param name="generator">The video generator.</param>
    /// <param name="sourceVideoData">The byte array containing the video data to use as input.</param>
    /// <param name="fileName">The filename for the video data.</param>
    /// <param name="prompt">The prompt to guide the video generation.</param>
    /// <param name="options">The video generation options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="generator"/>, <paramref name="fileName"/>, or <paramref name="prompt"/> is <see langword="null"/>.
    /// </exception>
    /// <returns>A <see cref="VideoGenerationOperation"/> representing the submitted video generation job.</returns>
    public static Task<VideoGenerationOperation> EditVideoAsync(
        this IVideoGenerator generator,
        ReadOnlyMemory<byte> sourceVideoData,
        string fileName,
        string prompt,
        VideoGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(generator);
        _ = Throw.IfNull(fileName);
        _ = Throw.IfNull(prompt);

        string mediaType = GetMediaTypeFromFileName(fileName);
        var dataContent = new DataContent(sourceVideoData, mediaType) { Name = fileName };

        return generator.GenerateAsync(
            new VideoGenerationRequest { Prompt = prompt, SourceVideo = dataContent, OperationKind = VideoOperationKind.Edit },
            options, cancellationToken);
    }

    /// <summary>
    /// Gets the media type based on the file extension.
    /// </summary>
    /// <param name="fileName">The filename to extract the media type from.</param>
    /// <returns>The inferred media type.</returns>
    private static string GetMediaTypeFromFileName(string fileName)
    {
        return MediaTypeMap.GetMediaType(fileName) ?? "video/mp4";
    }
}
