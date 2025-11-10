// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the result of an image generation request.</summary>
[Experimental("MEAI001")]
public class ImageGenerationResponse
{
    /// <summary>Initializes a new instance of the <see cref="ImageGenerationResponse"/> class.</summary>
    [JsonConstructor]
    public ImageGenerationResponse()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ImageGenerationResponse"/> class.</summary>
    /// <param name="contents">The contents for this response.</param>
    public ImageGenerationResponse(IList<AIContent>? contents)
    {
        Contents = contents;
    }

    /// <summary>Gets or sets the raw representation of the image generation response from an underlying implementation.</summary>
    /// <remarks>
    /// If a <see cref="ImageGenerationResponse"/> is created to represent some underlying object from another object
    /// model, this property can be used to store that original object. This can be useful for debugging or
    /// for enabling a consumer to access the underlying object model if needed.
    /// </remarks>
    [JsonIgnore]
    public object? RawRepresentation { get; set; }

    /// <summary>
    /// Gets or sets the generated content items.
    /// </summary>
    /// <remarks>
    /// Content is typically <see cref="DataContent"/> for images streamed from the generator, or <see cref="UriContent"/> for remotely hosted images, but
    /// can also be provider-specific content types that represent the generated images.
    /// </remarks>
    [AllowNull]
    public IList<AIContent> Contents
    {
        get => field ??= [];
        set;
    }

    /// <summary>Gets or sets usage details for the image generation response.</summary>
    public UsageDetails? Usage { get; set; }
}
