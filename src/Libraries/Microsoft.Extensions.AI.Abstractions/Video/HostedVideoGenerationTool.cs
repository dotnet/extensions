// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a hosted tool that can be specified to an AI service to enable it to perform video generation.</summary>
/// <remarks>
/// This tool does not itself implement video generation. It is a marker that can be used to inform a service
/// that the service is allowed to perform video generation if the service is capable of doing so.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIVideoGeneration, UrlFormat = DiagnosticIds.UrlFormat)]
public class HostedVideoGenerationTool : AITool
{
    /// <summary>Any additional properties associated with the tool.</summary>
    private IReadOnlyDictionary<string, object?>? _additionalProperties;

    /// <summary>
    /// Initializes a new instance of the <see cref="HostedVideoGenerationTool"/> class with the specified options.
    /// </summary>
    public HostedVideoGenerationTool()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="HostedVideoGenerationTool"/> class.</summary>
    /// <param name="additionalProperties">Any additional properties associated with the tool.</param>
    public HostedVideoGenerationTool(IReadOnlyDictionary<string, object?>? additionalProperties)
    {
        _additionalProperties = additionalProperties;
    }

    /// <inheritdoc />
    public override string Name => "video_generation";

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, object?> AdditionalProperties => _additionalProperties ?? base.AdditionalProperties;

    /// <summary>
    /// Gets or sets the options used to configure video generation.
    /// </summary>
    public VideoGenerationOptions? Options { get; set; }
}
