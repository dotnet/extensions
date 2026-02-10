// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a tool call request to a MCP server.
/// </summary>
/// <remarks>
/// <para>
/// This content type is used to represent an invocation of an MCP server tool by a hosted service.
/// It is informational only and may appear as part of an approval request
/// to convey what is being approved, or as a record of which MCP server tool was invoked.
/// </para>
/// </remarks>
public sealed class McpServerToolCallContent : ToolCallContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="McpServerToolCallContent"/> class.
    /// </summary>
    /// <param name="callId">The tool call ID.</param>
    /// <param name="name">The tool name.</param>
    /// <param name="serverName">The MCP server name that hosts the tool.</param>
    /// <exception cref="ArgumentNullException"><paramref name="callId"/> or <paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="callId"/> or <paramref name="name"/> is empty or composed entirely of whitespace.</exception>
    /// <remarks>
    /// This content is informational only and may appear as part of an approval request
    /// to convey what is being approved, or as a record of which MCP server tool was invoked.
    /// </remarks>
    public McpServerToolCallContent(string callId, string name, string? serverName)
        : base(Throw.IfNullOrWhitespace(callId))
    {
        Name = Throw.IfNullOrWhitespace(name);
        ServerName = serverName;
    }

    /// <summary>
    /// Gets the name of the tool requested.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the name of the MCP server that hosts the tool.
    /// </summary>
    public string? ServerName { get; }

    /// <summary>
    /// Gets or sets the arguments requested to be provided to the tool.
    /// </summary>
    public IDictionary<string, object?>? Arguments { get; set; }
}
