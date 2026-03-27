// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Text.Json.Serialization;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the options for a video generation request.</summary>
[Experimental(DiagnosticIds.Experiments.AIVideoGeneration, UrlFormat = DiagnosticIds.UrlFormat)]
public class VideoGenerationOptions
{
    /// <summary>Initializes a new instance of the <see cref="VideoGenerationOptions"/> class.</summary>
    public VideoGenerationOptions()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="VideoGenerationOptions"/> class, performing a shallow copy of all properties from <paramref name="other"/>.</summary>
    protected VideoGenerationOptions(VideoGenerationOptions? other)
    {
        if (other is null)
        {
            return;
        }

        AdditionalProperties = other.AdditionalProperties?.Clone();
        AspectRatio = other.AspectRatio;
        Count = other.Count;
        Duration = other.Duration;
        FramesPerSecond = other.FramesPerSecond;
        GenerateAudio = other.GenerateAudio;
        MediaType = other.MediaType;
        ModelId = other.ModelId;
        RawRepresentationFactory = other.RawRepresentationFactory;
        ResponseFormat = other.ResponseFormat;
        Seed = other.Seed;
        VideoSize = other.VideoSize;
    }

    /// <summary>
    /// Gets or sets the desired aspect ratio for the generated video (e.g., "16:9", "9:16", "1:1").
    /// </summary>
    public string? AspectRatio { get; set; }

    /// <summary>
    /// Gets or sets the number of videos to generate.
    /// </summary>
    public int? Count { get; set; }

    /// <summary>
    /// Gets or sets the desired duration for the generated video.
    /// </summary>
    /// <remarks>
    /// If a provider only supports fixed durations, the closest supported duration is used.
    /// </remarks>
    public TimeSpan? Duration { get; set; }

    /// <summary>
    /// Gets or sets the desired frames per second for the generated video.
    /// </summary>
    public int? FramesPerSecond { get; set; }

    /// <summary>
    /// Gets or sets whether to generate synchronized audio alongside the video.
    /// </summary>
    public bool? GenerateAudio { get; set; }

    /// <summary>
    /// Gets or sets the media type (also known as MIME type) of the generated video.
    /// </summary>
    public string? MediaType { get; set; }

    /// <summary>
    /// Gets or sets the model ID to use for video generation.
    /// </summary>
    public string? ModelId { get; set; }

    /// <summary>
    /// Gets or sets a callback responsible for creating the raw representation of the video generation options from an underlying implementation.
    /// </summary>
    /// <remarks>
    /// The underlying <see cref="IVideoGenerator" /> implementation can have its own representation of options.
    /// When <see cref="IVideoGenerator.GenerateAsync" /> is invoked with a <see cref="VideoGenerationOptions" />,
    /// that implementation can convert the provided options into its own representation in order to use it while performing
    /// the operation. For situations where a consumer knows which concrete <see cref="IVideoGenerator" /> is being used
    /// and how it represents options, a new instance of that implementation-specific options type can be returned by this
    /// callback for the <see cref="IVideoGenerator" /> implementation to use instead of creating a new instance.
    /// Such implementations might mutate the supplied options instance further based on other settings supplied on this
    /// <see cref="VideoGenerationOptions" /> instance or from other inputs, therefore, it is <b>strongly recommended</b> to not
    /// return shared instances and instead make the callback return a new instance on each call.
    /// This is typically used to set an implementation-specific setting that isn't otherwise exposed from the strongly typed
    /// properties on <see cref="VideoGenerationOptions" />.
    /// </remarks>
    [JsonIgnore]
    public Func<IVideoGenerator, object?>? RawRepresentationFactory { get; set; }

    /// <summary>
    /// Gets or sets the response format of the generated video.
    /// </summary>
    public VideoGenerationResponseFormat? ResponseFormat { get; set; }

    /// <summary>
    /// Gets or sets a seed value for reproducible generation.
    /// </summary>
    public int? Seed { get; set; }

    /// <summary>
    /// Gets or sets the size (resolution) of the generated video.
    /// </summary>
    /// <remarks>
    /// If a provider only supports fixed sizes, the closest supported size is used.
    /// </remarks>
    public Size? VideoSize { get; set; }

    /// <summary>Gets or sets any additional properties associated with the options.</summary>
    /// <remarks>
    /// This dictionary can be used to pass provider-specific settings that are not covered by
    /// the strongly typed properties on this class. Refer to provider documentation for supported keys.
    /// Unknown keys are typically forwarded as-is to the provider's API request body.
    /// </remarks>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <summary>Produces a clone of the current <see cref="VideoGenerationOptions"/> instance.</summary>
    /// <returns>A clone of the current <see cref="VideoGenerationOptions"/> instance.</returns>
    public virtual VideoGenerationOptions Clone() => new(this);
}

/// <summary>
/// Represents the requested response format of the generated video.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AIVideoGeneration, UrlFormat = DiagnosticIds.UrlFormat)]
public enum VideoGenerationResponseFormat
{
    /// <summary>
    /// The generated video is returned as a URI pointing to the video resource.
    /// </summary>
    Uri,

    /// <summary>
    /// The generated video is returned as in-memory video data.
    /// </summary>
    Data,

    /// <summary>
    /// The generated video is returned as a hosted resource identifier, which can be used to retrieve the video later.
    /// </summary>
    Hosted,
}
