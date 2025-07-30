// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents the mode in which approval is required for tool calls to a hosted MCP server.
/// </summary>
public class HostedMcpServerToolApprovalMode
{
    /// <summary>
    /// Gets the mode that indicates that all tool calls to a hosted MCP server always require user approval.
    /// </summary>
    public static HostedMcpServerToolApprovalMode Always { get; } = new AlwaysApprovalMode();

    /// <summary>
    /// Gets the mode that indicates that all tool calls to a hosted MCP server never require user approval.
    /// </summary>
    public static HostedMcpServerToolApprovalMode Never { get; } = new NeverApprovalMode();

    /// <summary>
    /// Initializes a new instance of the <see cref="HostedMcpServerToolApprovalMode"/> class.
    /// </summary>
    /// <param name="require">The list of tools that require approval for.</param>
    /// <param name="notRequire">The list of tools that do not require approval for.</param>
    public HostedMcpServerToolApprovalMode(IList<string> require, IList<string> notRequire)
    {
        Require = Throw.IfNull(require);
        NotRequire = Throw.IfNull(notRequire);
    }

    private HostedMcpServerToolApprovalMode()
    {
    }

    /// <summary>
    /// Gets the list of tools that require user approval for calls to a hosted MCP server.
    /// </summary>
    public IList<string>? Require { get; }

    /// <summary>
    /// Gets the list of tools that do not require user approval for calls to a hosted MCP server.
    /// </summary>
    public IList<string>? NotRequire { get; }

    private sealed class AlwaysApprovalMode : HostedMcpServerToolApprovalMode;

    private sealed class NeverApprovalMode : HostedMcpServerToolApprovalMode;
}
