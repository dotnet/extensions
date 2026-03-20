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

    /// <summary>Initializes a new instance of the <see cref="VideoGenerationRequest"/> class.</summary>
    /// <param name="prompt">The prompt to guide the video generation.</param>
    /// <param name="originalMedia">The original media (images or videos) to base edits on.</param>
    public VideoGenerationRequest(string prompt, IEnumerable<AIContent>? originalMedia)
    {
        Prompt = prompt;
        OriginalMedia = originalMedia;
    }

    /// <summary>Gets or sets the prompt to guide the video generation.</summary>
    public string? Prompt { get; set; }

    /// <summary>
    /// Gets or sets the original media (images or videos) to use as input for the video generation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The interpretation of this property depends on the content type of the media and the capabilities
    /// of the underlying provider. Common behaviors include:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// <b>Image content</b> (e.g., <c>image/png</c>, <c>image/jpeg</c>): Used as a reference image to guide new video
    /// generation. The provider creates a video inspired by or based on the image. Supported by most providers.
    /// </description></item>
    /// <item><description>
    /// <b>Video content</b> (e.g., <c>video/mp4</c>): Used as a source video for editing or remixing. The provider
    /// modifies the existing video according to the <see cref="Prompt"/>. Not all providers support video editing.
    /// </description></item>
    /// </list>
    /// <para>
    /// If this property is <see langword="null"/> or empty, the request is treated as a text-to-video generation
    /// using only the <see cref="Prompt"/>.
    /// </para>
    /// <para>
    /// Provider-specific scenarios such as video continuations or character consistency can be controlled
    /// via <see cref="VideoGenerationOptions.AdditionalProperties"/>. Refer to the provider documentation
    /// for supported keys.
    /// </para>
    /// </remarks>
    public IEnumerable<AIContent>? OriginalMedia { get; set; }
}
