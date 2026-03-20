// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents the invocation of a video generation tool call by a hosted service.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AIVideoGeneration, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class VideoGenerationToolCallContent : ToolCallContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VideoGenerationToolCallContent"/> class.
    /// </summary>
    /// <param name="callId">The tool call ID.</param>
    public VideoGenerationToolCallContent(string callId)
        : base(callId)
    {
    }
}
