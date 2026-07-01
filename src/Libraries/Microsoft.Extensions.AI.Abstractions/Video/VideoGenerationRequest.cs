// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a request for video generation.</summary>
[Experimental(DiagnosticIds.Experiments.AIVideoGeneration, UrlFormat = DiagnosticIds.UrlFormat)]
public class VideoGenerationRequest
{
    /// <summary>Initializes a new instance of the <see cref="VideoGenerationRequest"/> class.</summary>
    public VideoGenerationRequest()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="VideoGenerationRequest"/> class.</summary>
    /// <param name="prompt">The prompt to guide the video generation.</param>
    public VideoGenerationRequest(string prompt)
    {
        Prompt = prompt;
    }

    /// <summary>Gets or sets the prompt to guide the video generation.</summary>
    public string? Prompt { get; set; }

    /// <summary>Gets or sets a negative prompt describing what to avoid in the generated video.</summary>
    public string? NegativePrompt { get; set; }

    /// <summary>Gets or sets the kind of video operation to perform.</summary>
    /// <remarks>
    /// Defaults to <see cref="VideoOperationKind.Create"/>. Set to <see cref="VideoOperationKind.Edit"/> or
    /// <see cref="VideoOperationKind.Extend"/> when working with an existing video referenced by
    /// <see cref="SourceVideoId"/> or <see cref="SourceVideo"/>.
    /// </remarks>
    public VideoOperationKind OperationKind { get; set; }

    /// <summary>Gets or sets the provider-specific ID of an existing video to edit or extend.</summary>
    /// <remarks>
    /// This is typically the <see cref="VideoGenerationOperation.OperationId"/> of a previously completed
    /// video generation. Use <see cref="VideoGenerationOperation.CreateEditRequest"/> or
    /// <see cref="VideoGenerationOperation.CreateExtensionRequest"/> to create a request with this property set.
    /// </remarks>
    public string? SourceVideoId { get; set; }

    /// <summary>Gets or sets the starting frame image for image-to-video generation.</summary>
    /// <remarks>
    /// When provided with <see cref="VideoOperationKind.Create"/>, the provider uses this image as the
    /// initial frame from which the video is generated. Typically an image content such as
    /// <see cref="DataContent"/> or <see cref="UriContent"/> with an image media type.
    /// </remarks>
    public AIContent? StartFrame { get; set; }

    /// <summary>Gets or sets the ending frame image for video interpolation.</summary>
    /// <remarks>
    /// When provided alongside <see cref="StartFrame"/>, providers that support frame interpolation
    /// generate a video that transitions from <see cref="StartFrame"/> to this ending frame.
    /// Not all providers support this feature.
    /// </remarks>
    public AIContent? EndFrame { get; set; }

    /// <summary>Gets or sets reference images for style or subject guidance.</summary>
    /// <remarks>
    /// Reference images influence the visual style or subject matter of the generated video without
    /// being used as literal frames. For example, a provider may use these for style transfer or
    /// subject consistency. Not all providers support this feature.
    /// </remarks>
    public IList<AIContent>? ReferenceImages { get; set; }

    /// <summary>Gets or sets the source video content for editing.</summary>
    /// <remarks>
    /// Used when <see cref="OperationKind"/> is <see cref="VideoOperationKind.Edit"/> and the source
    /// video is provided as content rather than by ID. Typically a <see cref="DataContent"/> or
    /// <see cref="UriContent"/> with a video media type. To reference a previously generated video
    /// by its ID, use <see cref="SourceVideoId"/> instead.
    /// </remarks>
    public AIContent? SourceVideo { get; set; }
}
