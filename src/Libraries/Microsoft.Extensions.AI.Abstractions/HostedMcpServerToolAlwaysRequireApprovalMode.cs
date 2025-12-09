// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Indicates that approval is always required for tool calls to a hosted MCP server.
/// </summary>
/// <remarks>
/// Use <see cref="HostedMcpServerToolApprovalMode.AlwaysRequire"/> to get an instance of <see cref="HostedMcpServerToolAlwaysRequireApprovalMode"/>.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.McpServers, UrlFormat = DiagnosticIds.UrlFormat, Message = DiagnosticIds.Experiments.McpServersMessage)]
[DebuggerDisplay(nameof(AlwaysRequire))]
public sealed class HostedMcpServerToolAlwaysRequireApprovalMode : HostedMcpServerToolApprovalMode
{
    /// <summary>Initializes a new instance of the <see cref="HostedMcpServerToolAlwaysRequireApprovalMode"/> class.</summary>
    /// <remarks>Use <see cref="HostedMcpServerToolApprovalMode.AlwaysRequire"/> to get an instance of <see cref="HostedMcpServerToolAlwaysRequireApprovalMode"/>.</remarks>
    public HostedMcpServerToolAlwaysRequireApprovalMode()
    {
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is HostedMcpServerToolAlwaysRequireApprovalMode;

    /// <inheritdoc/>
    public override int GetHashCode() => typeof(HostedMcpServerToolAlwaysRequireApprovalMode).GetHashCode();
}
