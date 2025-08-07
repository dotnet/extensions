// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a single streaming response chunk from an <see cref="IImageClient"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ImageResponseUpdate"/> is so named because it represents updates
/// that layer on each other to form a single image response. This enables streaming
/// image generation where partial images or progress updates can be received.
/// </para>
/// <para>
/// The relationship between <see cref="ImageResponse"/> and <see cref="ImageResponseUpdate"/> is
/// similar to the relationship between <see cref="ChatResponse"/> and <see cref="ChatResponseUpdate"/>.
/// Multiple updates can be combined to form a complete response.
/// </para>
/// </remarks>
[DebuggerDisplay("ImageResponseUpdate: {Contents?.Count ?? 0} content(s)")]
[Experimental("MEAI001")]
public class ImageResponseUpdate
{
    /// <summary>The response update content items.</summary>
    private IList<AIContent>? _contents;

    /// <summary>Initializes a new instance of the <see cref="ImageResponseUpdate"/> class.</summary>
    [JsonConstructor]
    public ImageResponseUpdate()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ImageResponseUpdate"/> class.</summary>
    /// <param name="contents">The contents of the update.</param>
    public ImageResponseUpdate(IList<AIContent>? contents)
    {
        _contents = contents;
    }

    /// <summary>Gets or sets the image response update content items.</summary>
    [AllowNull]
    public IList<AIContent> Contents
    {
        get => _contents ??= [];
        set => _contents = value;
    }

    /// <summary>Gets or sets the raw representation of the response update from an underlying implementation.</summary>
    /// <remarks>
    /// If an <see cref="ImageResponseUpdate"/> is created to represent some underlying object from another object
    /// model, this property can be used to store that original object. This can be useful for debugging or
    /// for enabling a consumer to access the underlying object model if needed.
    /// </remarks>
    [JsonIgnore]
    public object? RawRepresentation { get; set; }

    /// <summary>Gets or sets additional properties for the update.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <summary>Gets or sets the ID of the response of which this update is a part.</summary>
    public string? ResponseId { get; set; }

    /// <summary>Gets or sets a timestamp for the response update.</summary>
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>Gets or sets the model ID associated with this response update.</summary>
    public string? ModelId { get; set; }

    /// <summary>Gets or sets an indication of the completion status of the image generation.</summary>
    /// <remarks>
    /// This can indicate whether the generation is in progress, completed successfully,
    /// or encountered an error.
    /// </remarks>
    public string? CompletionStatus { get; set; }
}
