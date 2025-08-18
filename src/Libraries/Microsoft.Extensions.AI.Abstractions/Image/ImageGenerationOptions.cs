﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the options for an image generation request.</summary>
[Experimental("MEAI001")]
public class ImageGenerationOptions
{
    /// <summary>
    /// Gets or sets the number of images to generate.
    /// </summary>
    public int? Count { get; set; }

    /// <summary>
    /// Gets or sets the size of the generated image.
    /// </summary>
    /// <remarks>
    /// If a provider only supports fixed sizes the closest supported size will be used.
    /// </remarks>
    public Size? ImageSize { get; set; }

    /// <summary>
    /// Gets or sets the media type (also known as MIME type) of the generated image.
    /// </summary>
    public string? MediaType { get; set; }

    /// <summary>
    /// Gets or sets the model ID to use for image generation.
    /// </summary>
    public string? ModelId { get; set; }

    /// <summary>
    /// Gets or sets a callback responsible for creating the raw representation of the image generation options from an underlying implementation.
    /// </summary>
    /// <remarks>
    /// The underlying <see cref="IImageGenerator" /> implementation may have its own representation of options.
    /// When <see cref="IImageGenerator.GenerateAsync" /> is invoked with an <see cref="ImageGenerationOptions" />,
    /// that implementation may convert the provided options into its own representation in order to use it while performing
    /// the operation. For situations where a consumer knows  which concrete <see cref="IImageGenerator" /> is being used
    /// and how it represents options, a new instance of that implementation-specific options type may be returned by this
    /// callback, for the <see cref="IImageGenerator" />implementation to use instead of creating a new instance.
    /// Such implementations may mutate the supplied options instance further based on other settings supplied on this
    /// <see cref="ImageGenerationOptions" /> instance or from other inputs, therefore, it is <b>strongly recommended</b> to not
    /// return shared instances and instead make the callback return a new instance on each call.
    /// This is typically used to set an implementation-specific setting that isn't otherwise exposed from the strongly-typed
    /// properties on <see cref="ImageGenerationOptions" />.
    /// </remarks>
    [JsonIgnore]
    public Func<IImageGenerator, object?>? RawRepresentationFactory { get; set; }

    /// <summary>
    /// Gets or sets the response format of the generated image.
    /// </summary>
    public ImageGenerationResponseFormat? ResponseFormat { get; set; }

    /// <summary>Produces a clone of the current <see cref="ImageGenerationOptions"/> instance.</summary>
    /// <returns>A clone of the current <see cref="ImageGenerationOptions"/> instance.</returns>
    public virtual ImageGenerationOptions Clone()
    {
        ImageGenerationOptions options = new()
        {
            Count = Count,
            MediaType = MediaType,
            ImageSize = ImageSize,
            ModelId = ModelId,
            RawRepresentationFactory = RawRepresentationFactory,
            ResponseFormat = ResponseFormat
        };

        return options;
    }
}

/// <summary>
/// Represents the requested response format of the generated image.
/// </summary>
/// <remarks>
/// Not all implementations support all response formats and this value may be ignored by the implementation if not supported.
/// </remarks>
[Experimental("MEAI001")]
public enum ImageGenerationResponseFormat
{
    /// <summary>
    /// The generated image is returned as a URI pointing to the image resource.
    /// </summary>
    Uri,

    /// <summary>
    /// The generated image is returned as in-memory image data.
    /// </summary>
    Data,

    /// <summary>
    /// The generated image is returned as a hosted resource identifier, which can be used to retrieve the image later.
    /// </summary>
    Hosted,
}
