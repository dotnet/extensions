// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Indicates that approval is never required for tool calls to a hosted MCP server.
/// </summary>
/// <remarks>
/// Use <see cref="HostedMcpServerToolApprovalMode.NeverRequire"/> to get an instance of <see cref="HostedMcpServerToolNeverRequireApprovalMode"/>.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.McpServers, Message = DiagnosticIds.Experiments.McpServersMessage, UrlFormat = DiagnosticIds.UrlFormat)]
[DebuggerDisplay(nameof(NeverRequire))]
public sealed class HostedMcpServerToolNeverRequireApprovalMode : HostedMcpServerToolApprovalMode
{
    /// <summary>Initializes a new instance of the <see cref="HostedMcpServerToolNeverRequireApprovalMode"/> class.</summary>
    /// <remarks>Use <see cref="HostedMcpServerToolApprovalMode.NeverRequire"/> to get an instance of <see cref="HostedMcpServerToolNeverRequireApprovalMode"/>.</remarks>
    public HostedMcpServerToolNeverRequireApprovalMode()
    {
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is HostedMcpServerToolNeverRequireApprovalMode;

    /// <inheritdoc/>
    public override int GetHashCode() => typeof(HostedMcpServerToolNeverRequireApprovalMode).GetHashCode();
}
