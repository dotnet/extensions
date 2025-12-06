// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a tool call request to a MCP server.
/// </summary>
/// <remarks>
/// This content type is used to represent an invocation of an MCP server tool by a hosted service.
/// It is informational only.
/// </remarks>
[Experimental("MEAI001")]
public sealed class McpServerToolCallContent : ServiceActionContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="McpServerToolCallContent"/> class.
    /// </summary>
    /// <param name="id">The tool call ID.</param>
    /// <param name="toolName">The tool name.</param>
    /// <param name="serverName">The MCP server name that hosts the tool.</param>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> or <paramref name="toolName"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="id"/> or <paramref name="toolName"/> is empty or composed entirely of whitespace.</exception>
    public McpServerToolCallContent(string id, string toolName, string? serverName)
        : base(id)
    {
        ToolName = Throw.IfNullOrWhitespace(toolName);
        ServerName = serverName;
    }

    /// <summary>
    /// Gets the name of the tool called.
    /// </summary>
    public string ToolName { get; }

    /// <summary>
    /// Gets the name of the MCP server that hosts the tool.
    /// </summary>
    public string? ServerName { get; }

    /// <summary>
    /// Gets or sets the arguments used for the tool call.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Arguments { get; set; }
}
