// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a tool call request to a MCP server.
/// </summary>
/// <remarks>
/// <para>
/// This content type is used to represent an invocation of an MCP server tool by a hosted service.
/// It is generally considered informational only, and is provided either as part of an approval request
/// to convey what is being approved or as information about what tool was invoked elsewhere in the stack.
/// </para>
/// </remarks>
public sealed class McpServerToolCallContent : FunctionCallContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="McpServerToolCallContent"/> class.
    /// </summary>
    /// <param name="callId">The tool call ID.</param>
    /// <param name="name">The tool name.</param>
    /// <param name="serverName">The MCP server name that hosts the tool.</param>
    /// <exception cref="ArgumentNullException"><paramref name="callId"/> or <paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="callId"/> or <paramref name="name"/> is empty or composed entirely of whitespace.</exception>
    public McpServerToolCallContent(string callId, string name, string? serverName)
        : base(Throw.IfNullOrWhitespace(callId), Throw.IfNullOrWhitespace(name))
    {
        ServerName = serverName;
        InformationalOnly = true;
    }

    /// <summary>
    /// Gets the name of the MCP server that hosts the tool.
    /// </summary>
    public string? ServerName { get; }
}
