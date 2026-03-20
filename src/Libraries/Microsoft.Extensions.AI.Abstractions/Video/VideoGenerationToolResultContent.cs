// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a video generation tool call invocation by a hosted service.
/// </summary>
/// <remarks>
/// This content type represents when a hosted AI service invokes a video generation tool.
/// It is informational only and represents the call itself, not the result.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIVideoGeneration, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class VideoGenerationToolResultContent : ToolResultContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VideoGenerationToolResultContent"/> class.
    /// </summary>
    /// <param name="callId">The tool call ID.</param>
    public VideoGenerationToolResultContent(string callId)
        : base(callId)
    {
    }

    /// <summary>
    /// Gets or sets the generated content items.
    /// </summary>
    /// <remarks>
    /// Content is typically <see cref="DataContent"/> for videos streamed from the tool, or <see cref="UriContent"/> for remotely hosted videos, but
    /// can also be provider-specific content types that represent the generated videos.
    /// </remarks>
    public IList<AIContent>? Outputs { get; set; }
}
