// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a hosted tool that can be specified to an AI service to enable it to perform web searches.</summary>
/// <remarks>
/// This tool does not itself implement web searches. It is a marker that can be used to inform a service
/// that the service is allowed to perform web searches if the service is capable of doing so.
/// </remarks>
public class HostedWebSearchTool : AITool
{
    /// <summary>Any additional properties associated with the tool.</summary>
    private IReadOnlyDictionary<string, object?>? _additionalProperties;

    /// <summary>Initializes a new instance of the <see cref="HostedWebSearchTool"/> class.</summary>
    public HostedWebSearchTool()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="HostedWebSearchTool"/> class.</summary>
    /// <param name="additionalProperties">Any additional properties associated with the tool.</param>
    public HostedWebSearchTool(IReadOnlyDictionary<string, object?>? additionalProperties)
    {
        _additionalProperties = additionalProperties;
    }

    /// <inheritdoc />
    public override string Name => "web_search";

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, object?> AdditionalProperties => _additionalProperties ?? base.AdditionalProperties;
}
