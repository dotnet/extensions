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
public class HostedMcpServerTool : AITool
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HostedMcpServerTool"/> class.
    /// </summary>
    /// <param name="serverName">The name of the remote MCP server.</param>
    /// <param name="url">The URL of the remote MCP server.</param>
    /// <exception cref="ArgumentNullException"><paramref name="serverName"/> or <paramref name="url"/> are <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="serverName"/> is empty or composed entirely of whitespace.</exception>
    public HostedMcpServerTool(string serverName, [StringSyntax(StringSyntaxAttribute.Uri)] string url)
        : this(serverName, new Uri(Throw.IfNull(url)))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HostedMcpServerTool"/> class.
    /// </summary>
    /// <param name="serverName">The name of the remote MCP server.</param>
    /// <param name="url">The URL of the remote MCP server.</param>
    /// <exception cref="ArgumentNullException"><paramref name="serverName"/> or <paramref name="url"/> are <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="serverName"/> is empty or composed entirely of whitespace.</exception>
    public HostedMcpServerTool(string serverName, Uri url)
    {
        ServerName = Throw.IfNullOrWhitespace(serverName);
        Url = Throw.IfNull(url);
    }

    /// <summary>
    /// Gets the name of the remote MCP server that is used to identify it.
    /// </summary>
    public string ServerName { get; }

    /// <summary>
    /// Gets the URL of the remote MCP server.
    /// </summary>
    public Uri Url { get; }

    /// <summary>
    /// Gets or sets the description of the remote MCP server, used to provide more context to the AI service.
    /// </summary>
    public string? ServerDescription { get; set; }

    /// <summary>
    /// Gets or sets the list of tools allowed to be used by the AI service.
    /// </summary>
    /// <remarks>
    /// The default value is <see langword="null"/>, which allows any tool to be used.
    /// </remarks>
    public IList<string>? AllowedTools { get; set; }

    /// <summary>
    /// Gets or sets the approval mode that indicates when the AI service should require user approval for tool calls to the remote MCP server.
    /// </summary>
    /// <remarks>
    /// <para>
    /// You can set this property to <see cref="HostedMcpServerToolApprovalMode.AlwaysRequire"/> to require approval for all tool calls, 
    /// or to <see cref="HostedMcpServerToolApprovalMode.NeverRequire"/> to never require approval.
    /// </para>
    /// <para>
    /// The default value is <see langword="null"/>, which some providers may treat the same as <see cref="HostedMcpServerToolApprovalMode.AlwaysRequire"/>.
    /// </para>
    /// <para>
    /// The underlying provider is not guaranteed to support or honor the approval mode.
    /// </para>
    /// </remarks>
    public HostedMcpServerToolApprovalMode? ApprovalMode { get; set; }

    /// <summary>
    /// Gets or sets the HTTP headers that the AI service should use when calling the remote MCP server.
    /// </summary>
    /// <remarks>
    /// This property is useful for specifying the authentication header or other headers required by the MCP server.
    /// </remarks>
    public IDictionary<string, string>? Headers { get; set; }
}
