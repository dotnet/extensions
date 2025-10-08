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
public sealed class McpServerToolCallContent : AIContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="McpServerToolCallContent"/> class.
    /// </summary>
    /// <param name="callId">The tool call ID.</param>
    /// <exception cref="ArgumentNullException"><paramref name="callId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="callId"/> is empty or composed entirely of whitespace.</exception>
    public McpServerToolCallContent(string callId)
    {
        CallId = Throw.IfNullOrWhitespace(callId);
    }

    /// <summary>
    /// Gets the tool call ID.
    /// </summary>
    public string CallId { get; }

    /// <summary>
    /// Gets or sets the name of the tool called.
    /// </summary>
    public string? ToolName { get; set; }

    /// <summary>
    /// Gets or sets the name of the MCP server.
    /// </summary>
    public string? ServerName { get; set; }

    /// <summary>
    /// Gets or sets the arguments used for the tool call.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Arguments { get; set; }
}
