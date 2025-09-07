// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a tool call request to a MCP server.
/// </summary>
public class McpServerToolCallContent : AIContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="McpServerToolCallContent"/> class.
    /// </summary>
    /// <param name="callId">The tool call ID.</param>
    /// <param name="toolName">The tool name.</param>
    /// <param name="serverName">The MCP server name.</param>
    /// <exception cref="ArgumentNullException"><paramref name="callId"/>, <paramref name="toolName"/>, or <paramref name="serverName"/> are <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="callId"/>, <paramref name="toolName"/>, or <paramref name="serverName"/> are empty or composed entirely of whitespace.</exception>
    public McpServerToolCallContent(string callId, string toolName, string serverName)
    {
        CallId = Throw.IfNullOrWhitespace(callId);
        ToolName = Throw.IfNullOrWhitespace(toolName);
        ServerName = Throw.IfNullOrWhitespace(serverName);
    }

    /// <summary>
    /// Gets the tool call ID.
    /// </summary>
    public string CallId { get; }

    /// <summary>
    /// Gets the name of the tool called.
    /// </summary>
    public string ToolName { get; }

    /// <summary>
    /// Gets the name of the MCP server.
    /// </summary>
    public string ServerName { get; }

    /// <summary>
    /// Gets or sets the arguments used for the tool call.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Arguments { get; set; }
}
