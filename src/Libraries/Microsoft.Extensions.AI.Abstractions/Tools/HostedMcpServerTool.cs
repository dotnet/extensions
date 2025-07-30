// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a hosted MCP server tool that can be specified to an AI service.
/// </summary>
[Experimental("MEAI001")]
public class HostedMcpServerTool : AITool
{
    private readonly string _name;
    private readonly string _description;

    /// <summary>
    /// Initializes a new instance of the <see cref="HostedMcpServerTool"/> class.
    /// </summary>
    /// <param name="name">The name of the remote MCP server.</param>
    /// <param name="url">The URL of the remote MCP server.</param>
    /// <param name="description">The description of the remote MCP server.</param>
    public HostedMcpServerTool(string name, Uri url, string? description = null)
    {
        _name = Throw.IfNullOrWhitespace(name);
        _description = description ?? string.Empty;

        Url = Throw.IfNull(url);
    }

    /// <summary>
    /// Gets the name of the remote MCP server that is used to identify it.
    /// </summary>
    public override string Name => _name;

    /// <summary>
    /// Gets the URL of the remote MCP server.
    /// </summary>
    public Uri Url { get; }

    /// <summary>
    /// Gets the description of the remote MCP server, used to provide more context to the AI service.
    /// </summary>
    public override string Description => _description;

    /// <summary>
    /// Gets or sets the list of tools allowed to be used by the AI service.
    /// </summary>
    public IList<string>? AllowedTools { get; set; }

    /// <summary>
    /// Gets or sets the approval mode that indicates when the AI service should require user approval for tool calls to the remote MCP server.
    /// </summary>
    /// <remarks>
    /// You can set this property to <see cref="HostedMcpServerToolApprovalMode.Always"/> to require approval for all tool calls, 
    /// or to <see cref="HostedMcpServerToolApprovalMode.Never"/> to never require approval.
    /// </remarks>
    public HostedMcpServerToolApprovalMode? ApprovalMode { get; set; }

    /// <summary>
    /// Gets or sets the HTTP headers that the AI service should use when making tool calls to the remote MCP server.
    /// </summary>
    /// <remarks>
    /// This property is useful for specifying authentication or other headers required by the MCP server.
    /// </remarks>
    public IDictionary<string, string>? Headers { get; set; }
}
