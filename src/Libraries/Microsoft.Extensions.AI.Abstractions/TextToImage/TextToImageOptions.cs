// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the options for an image generation request.</summary>
[Experimental("MEAI001")]
public class TextToImageOptions
{
    /// <summary>Gets or sets the content type of the image.</summary>
    public TextToImageContentType ContentType { get; set; } = TextToImageContentType.Uri;

    /// <summary>
    /// Gets or sets the number of images to generate.
    /// </summary>
    public int Count { get; set; } = 1;

    /// <summary>
    /// Gets or sets the guidance scale to use for image generation.  Not supported by all providers.
    /// </summary>
    public float? GuidanceScale { get; set; }

    /// <summary>
    /// Gets or sets the size of the generated image.
    /// If a provider only supports fixed sizes the closest supported size will be used.
    /// A value of default(Size) indicates the default for the provider should be used.
    /// </summary>
    public Size ImageSize { get; set; }

    /// <summary>
    /// Gets or sets the model ID to use for image generation.  Not supported by all providers.
    /// </summary>
    public string? ModelId { get; set; }

    /// <summary>
    /// Gets or sets the negative prompt to use for image generation.  Not supported by all providers.
    /// </summary>
    public string? NegativePrompt { get; set; }

    /// <summary>
    /// Gets or sets the diffusion step count to use for image generation.  Not supported by all providers.
    /// </summary>
    public int? Steps { get; set; }

    /// <summary>
    /// Gets or sets a callback responsible for creating the raw representation of the embedding generation options from an underlying implementation.
    /// </summary>
    /// <remarks>
    /// The underlying <see cref="ITextToImageClient" /> implementation may have its own representation of options.
    /// When <see cref="ITextToImageClient.GenerateImagesAsync" /> is invoked with an <see cref="TextToImageOptions" />,
    /// that implementation may convert the provided options into its own representation in order to use it while performing
    /// the operation. For situations where a consumer knows  which concrete <see cref="ITextToImageClient" /> is being used
    /// and how it represents options, a new instance of that implementation-specific options type may be returned by this
    /// callback, for the <see cref="ITextToImageClient" />implementation to use instead of creating a new instance.
    /// Such implementations may mutate the supplied options instance further based on other settings supplied on this
    /// <see cref="TextToImageOptions" /> instance or from other inputs, therefore, it is <b>strongly recommended</b> to not
    /// return shared instances and instead make the callback return a new instance on each call.
    /// This is typically used to set an implementation-specific setting that isn't otherwise exposed from the strongly-typed
    /// properties on <see cref="TextToImageOptions" />.
    /// </remarks>
    [JsonIgnore]
    public Func<ITextToImageClient, object?>? RawRepresentationFactory { get; set; }

    /// <summary>Produces a clone of the current <see cref="TextToImageOptions"/> instance.</summary>
    /// <returns>A clone of the current <see cref="TextToImageOptions"/> instance.</returns>
    public virtual TextToImageOptions Clone()
    {
        TextToImageOptions options = new()
        {
            ContentType = ContentType,
            Count = Count,
            GuidanceScale = GuidanceScale,
            ImageSize = ImageSize,
            ModelId = ModelId,
            NegativePrompt = NegativePrompt,
            Steps = Steps,
            RawRepresentationFactory = RawRepresentationFactory
        };

        return options;
    }
}

/// <summary>
/// Represents the requested content type of the generated image.
/// </summary>
[Experimental("MEAI001")]
public enum TextToImageContentType
{
    /// <summary>
    /// The generated image is returned as a URI pointing to the image resource.
    /// </summary>
    Uri,

    /// <summary>
    /// The generated image is returned as in-memory image data.
    /// </summary>
    Data
}
