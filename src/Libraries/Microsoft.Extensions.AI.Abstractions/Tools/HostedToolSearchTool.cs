// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a hosted tool that can be specified to an AI service to enable it to search for and selectively load tool definitions on demand.</summary>
/// <remarks>
/// This tool does not itself implement tool search. It is a marker that can be used to inform a service
/// that tool search should be enabled, reducing token usage by deferring full tool schema loading until the model requests it.
/// </remarks>
public class HostedToolSearchTool : AITool
{
    /// <summary>Any additional properties associated with the tool.</summary>
    private IReadOnlyDictionary<string, object?>? _additionalProperties;

    /// <summary>Initializes a new instance of the <see cref="HostedToolSearchTool"/> class.</summary>
    public HostedToolSearchTool()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="HostedToolSearchTool"/> class.</summary>
    /// <param name="additionalProperties">Any additional properties associated with the tool.</param>
    public HostedToolSearchTool(IReadOnlyDictionary<string, object?>? additionalProperties)
    {
        _additionalProperties = additionalProperties;
    }

    /// <inheritdoc />
    public override string Name => "tool_search";

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, object?> AdditionalProperties => _additionalProperties ?? base.AdditionalProperties;
}
