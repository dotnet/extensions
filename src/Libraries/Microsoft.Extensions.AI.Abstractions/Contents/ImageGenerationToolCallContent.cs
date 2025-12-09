// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents the invocation of an image generation tool call by a hosted service.
/// </summary>
[Experimental(DiagnosticIds.Experiments.ImageGeneration, Message = DiagnosticIds.Experiments.ImageGenerationMessage, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class ImageGenerationToolCallContent : AIContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImageGenerationToolCallContent"/> class.
    /// </summary>
    public ImageGenerationToolCallContent()
    {
    }

    /// <summary>
    /// Gets or sets the unique identifier of the image generation item.
    /// </summary>
    public string? ImageId { get; set; }
}
