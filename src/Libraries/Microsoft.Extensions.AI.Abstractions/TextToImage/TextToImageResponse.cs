// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

#pragma warning disable EA0011 // Consider removing unnecessary conditional access operators

namespace Microsoft.Extensions.AI;

/// <summary>Represents the result of an image generation request.</summary>
[Experimental("MEAI001")]
public class TextToImageResponse
{
    /// <summary>The content items in the generated text response.</summary>
    private IList<AIContent>? _contents;

    /// <summary>Initializes a new instance of the <see cref="TextToImageResponse"/> class.</summary>
    [JsonConstructor]
    public TextToImageResponse()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="TextToImageResponse"/> class.</summary>
    /// <param name="contents">The contents for this response.</param>
    /// <exception cref="ArgumentNullException"><paramref name="contents"/> is <see langword="null"/>.</exception>
    public TextToImageResponse(IList<AIContent> contents)
    {
        _contents = Throw.IfNull(contents);
    }

    /// <summary>Gets or sets the raw representation of the text to image response from an underlying implementation.</summary>
    /// <remarks>
    /// If a <see cref="TextToImageResponse"/> is created to represent some underlying object from another object
    /// model, this property can be used to store that original object. This can be useful for debugging or
    /// for enabling a consumer to access the underlying object model if needed.
    /// </remarks>
    [JsonIgnore]
    public object? RawRepresentation { get; set; }

    /// <summary>
    /// Gets or sets the generated content items.  Content will typically be DataContent for
    /// images streamed from the client or UriContent for remotely hosted images, but may also
    /// be provider specific content types that represent the generated images.
    /// </summary>
    [AllowNull]
    public IList<AIContent> Contents
    {
        get => _contents ??= [];
        set => _contents = value;
    }
}
