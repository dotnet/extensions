// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a tool call request to a hosted MCP server.
/// </summary>
[Experimental("MEAI001")]
public class HostedMcpServerToolCallContent : AIContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HostedMcpServerToolCallContent"/> class.
    /// </summary>
    /// <param name="callId">The tool call ID.</param>
    /// <param name="name">The tool name.</param>
    /// <param name="serverName">The MCP server name.</param>
    /// <param name="arguments">The arguments used for the tool call.</param>
    public HostedMcpServerToolCallContent(string callId, string name, string serverName, IReadOnlyDictionary<string, object?>? arguments = null)
    {
        CallId = Throw.IfNullOrWhitespace(callId);
        Name = Throw.IfNullOrWhitespace(name);
        ServerName = Throw.IfNullOrWhitespace(serverName);
        Arguments = arguments;
    }

    /// <summary>
    /// Gets the tool call ID.
    /// </summary>
    public string CallId { get; }

    /// <summary>
    /// Gets the name of the tool requested.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the name of the MCP server.
    /// </summary>
    public string ServerName { get; }

    /// <summary>
    /// Gets the arguments requested to be provided to the tool.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Arguments { get; }
}
