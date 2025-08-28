// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

#pragma warning disable EA0011 // Consider removing unnecessary conditional access operators

namespace Microsoft.Extensions.AI;

/// <summary>Represents the result of an image generation request.</summary>
[Experimental("MEAI001")]
public class ImageGenerationResponse
{
    /// <summary>The content items in the generated text response.</summary>
    private IList<AIContent>? _contents;

    /// <summary>Initializes a new instance of the <see cref="ImageGenerationResponse"/> class.</summary>
    [JsonConstructor]
    public ImageGenerationResponse()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ImageGenerationResponse"/> class.</summary>
    /// <param name="contents">The contents for this response.</param>
    public ImageGenerationResponse(IList<AIContent>? contents)
    {
        _contents = contents;
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
    /// Gets or sets the generated content items.  Content will typically be DataContent for
    /// images streamed from the generator or UriContent for remotely hosted images, but may also
    /// be provider specific content types that represent the generated images.
    /// </summary>
    [AllowNull]
    public IList<AIContent> Contents
    {
        get => _contents ??= [];
        set => _contents = value;
    }
}
