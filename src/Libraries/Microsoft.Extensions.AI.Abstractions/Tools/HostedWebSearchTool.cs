// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

/// <summary>Represents a hosted tool that can be specified to an AI service to enable it to perform web searches.</summary>
/// <remarks>
/// This tool does not itself implement web searches. It is a marker that can be used to inform a service
/// that the service is allowed to perform web searches if the service is capable of doing so.
/// </remarks>
public class HostedWebSearchTool : AITool
{
    /// <summary>Initializes a new instance of the <see cref="HostedWebSearchTool"/> class.</summary>
    public HostedWebSearchTool()
    {
    }
}
