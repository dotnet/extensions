// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a response to an MCP server tool approval request.
/// </summary>
[Experimental(DiagnosticIds.Experiments.McpServers, Message = DiagnosticIds.Experiments.McpServersMessage, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class McpServerToolApprovalResponseContent : UserInputResponseContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="McpServerToolApprovalResponseContent"/> class.
    /// </summary>
    /// <param name="id">The ID that uniquely identifies the MCP server tool approval request/response pair.</param>
    /// <param name="approved"><see langword="true"/> if the MCP server tool call is approved; otherwise, <see langword="false"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="id"/> is empty or composed entirely of whitespace.</exception>
    public McpServerToolApprovalResponseContent(string id, bool approved)
        : base(id)
    {
        Approved = approved;
    }

    /// <summary>
    /// Gets a value indicating whether the user approved the request.
    /// </summary>
    public bool Approved { get; }
}
