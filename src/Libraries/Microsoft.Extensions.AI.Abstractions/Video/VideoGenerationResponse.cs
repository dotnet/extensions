// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the result of a video generation request.</summary>
[Experimental(DiagnosticIds.Experiments.AIVideoGeneration, UrlFormat = DiagnosticIds.UrlFormat)]
public class VideoGenerationResponse
{
    /// <summary>Initializes a new instance of the <see cref="VideoGenerationResponse"/> class.</summary>
    [JsonConstructor]
    public VideoGenerationResponse()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="VideoGenerationResponse"/> class.</summary>
    /// <param name="contents">The contents for this response.</param>
    public VideoGenerationResponse(IList<AIContent>? contents)
    {
        Contents = contents;
    }

    /// <summary>Gets or sets the raw representation of the video generation response from an underlying implementation.</summary>
    /// <remarks>
    /// If a <see cref="VideoGenerationResponse"/> is created to represent some underlying object from another object
    /// model, this property can be used to store that original object. This can be useful for debugging or
    /// for enabling a consumer to access the underlying object model if needed.
    /// </remarks>
    [JsonIgnore]
    public object? RawRepresentation { get; set; }

    /// <summary>
    /// Gets or sets the generated content items.
    /// </summary>
    /// <remarks>
    /// Content is typically <see cref="DataContent"/> for videos streamed from the generator, or <see cref="UriContent"/> for remotely hosted videos, but
    /// can also be provider-specific content types that represent the generated videos.
    /// </remarks>
    [AllowNull]
    public IList<AIContent> Contents
    {
        get => field ??= [];
        set;
    }

    /// <summary>Gets or sets usage details for the video generation response.</summary>
    public UsageDetails? Usage { get; set; }

    /// <summary>Gets or sets the model ID used to generate the video.</summary>
    public string? ModelId { get; set; }

    /// <summary>Gets or sets any additional properties associated with the response.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }
}
