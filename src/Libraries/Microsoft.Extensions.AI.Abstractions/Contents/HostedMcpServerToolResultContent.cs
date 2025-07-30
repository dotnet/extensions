// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a tool call result from a hosted MCP server.
/// </summary>
[Experimental("MEAI001")]
public class HostedMcpServerToolResultContent : AIContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HostedMcpServerToolResultContent"/> class.
    /// </summary>
    /// <param name="callId">The tool call ID.</param>
    public HostedMcpServerToolResultContent(string callId)
    {
        CallId = Throw.IfNullOrWhitespace(callId);
    }

    /// <summary>
    /// Gets the tool call ID.
    /// </summary>
    public string CallId { get; }

    /// <summary>
    /// Gets or sets the output of the tool call.
    /// </summary>
    public string? Output { get; set; }

    /// <summary>
    /// Gets or sets the error of the tool call, if any.
    /// </summary>
    public string? Error { get; set; }
}
