// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a request for image generation.</summary>
[Experimental("MEAI001")]
public class ImageGenerationRequest
{
    /// <summary>Initializes a new instance of the <see cref="ImageGenerationRequest"/> class.</summary>
    public ImageGenerationRequest()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ImageGenerationRequest"/> class.</summary>
    /// <param name="prompt">The prompt to guide the image generation.</param>
    public ImageGenerationRequest(string prompt)
    {
        Prompt = prompt;
    }

    /// <summary>Initializes a new instance of the <see cref="ImageGenerationRequest"/> class.</summary>
    /// <param name="prompt">The prompt to guide the image generation.</param>
    /// <param name="originalImages">The original images to base edits on.</param>
    public ImageGenerationRequest(string prompt, IEnumerable<AIContent>? originalImages)
    {
        Prompt = prompt;
        OriginalImages = originalImages;
    }

    /// <summary>Gets or sets the prompt to guide the image generation.</summary>
    public string? Prompt { get; set; }

    /// <summary>
    /// Gets or sets the original images to base edits on.
    /// </summary>
    /// <remarks>
    /// If this property is set, the request will behave as an image edit operation.
    /// If this property is null or empty, the request will behave as a new image generation operation.
    /// </remarks>
    public IEnumerable<AIContent>? OriginalImages { get; set; }
}
