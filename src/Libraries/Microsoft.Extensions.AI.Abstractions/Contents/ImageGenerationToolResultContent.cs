// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents an image generation tool call invocation by a hosted service.
/// </summary>
/// <remarks>
/// This content type represents when a hosted AI service invokes an image generation tool.
/// It is informational only and represents the call itself, not the result.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIImageGeneration, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class ImageGenerationToolResultContent : ToolResultContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImageGenerationToolResultContent"/> class.
    /// </summary>
    /// <param name="callId">The tool call ID.</param>
    [JsonConstructor]
    public ImageGenerationToolResultContent(string callId)
        : base(callId)
    {
    }

    /// <summary>
    /// Gets or sets the generated content items.
    /// </summary>
    /// <remarks>
    /// Content is typically <see cref="DataContent"/> for images streamed from the tool, or <see cref="UriContent"/> for remotely hosted images, but
    /// can also be provider-specific content types that represent the generated images.
    /// </remarks>
    public IList<AIContent>? Outputs { get; set; }
}
