// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Specifies the kind of video generation operation to perform.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AIVideoGeneration, UrlFormat = DiagnosticIds.UrlFormat)]
public enum VideoOperationKind
{
    /// <summary>
    /// Create a new video from a text prompt, optionally guided by a starting frame image
    /// supplied via <see cref="VideoGenerationRequest.StartFrame"/>.
    /// </summary>
    Create,

    /// <summary>
    /// Edit an existing video identified by <see cref="VideoGenerationRequest.SourceVideoId"/>
    /// or provided via <see cref="VideoGenerationRequest.SourceVideo"/>.
    /// </summary>
    Edit,

    /// <summary>
    /// Extend an existing video identified by <see cref="VideoGenerationRequest.SourceVideoId"/>.
    /// </summary>
    Extend,
}
