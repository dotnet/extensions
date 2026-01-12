// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a tool call request to a MCP server.
/// </summary>
/// <remarks>
/// This content type is used to represent an invocation of an MCP server tool by a hosted service.
/// It is informational only.
/// </remarks>
[Experimental("MEAI001")]
public sealed class McpServerToolCallContent : FunctionCallContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="McpServerToolCallContent"/> class.
    /// </summary>
    /// <param name="callId">The tool call ID.</param>
    /// <param name="toolName">The tool name.</param>
    /// <param name="serverName">The MCP server name that hosts the tool.</param>
    /// <exception cref="ArgumentNullException"><paramref name="callId"/> or <paramref name="toolName"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="callId"/> or <paramref name="toolName"/> is empty or composed entirely of whitespace.</exception>
    [JsonConstructor]
    public McpServerToolCallContent(string callId, string toolName, string? serverName)
        : base(callId, toolName, arguments: null)
    {
        ServerName = serverName;
    }

    /// <summary>
    /// Gets the name of the tool called.
    /// </summary>
    public string ToolName => Name;

    /// <summary>
    /// Gets the name of the MCP server that hosts the tool.
    /// </summary>
    public string? ServerName { get; }
}
