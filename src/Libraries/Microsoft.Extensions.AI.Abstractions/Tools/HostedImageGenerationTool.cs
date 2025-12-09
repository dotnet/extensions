// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a hosted tool that can be specified to an AI service to enable it to perform image generation.</summary>
/// <remarks>
/// This tool does not itself implement image generation. It is a marker that can be used to inform a service
/// that the service is allowed to perform image generation if the service is capable of doing so.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.ImageGeneration, UrlFormat = DiagnosticIds.UrlFormat)]
public class HostedImageGenerationTool : AITool
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HostedImageGenerationTool"/> class with the specified options.
    /// </summary>
    public HostedImageGenerationTool()
    {
    }

    /// <summary>
    /// Gets or sets the options used to configure image generation.
    /// </summary>
    public ImageGenerationOptions? Options { get; set; }
}
