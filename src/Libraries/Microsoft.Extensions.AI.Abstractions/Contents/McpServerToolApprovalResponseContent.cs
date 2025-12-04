// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a response to an MCP server tool approval request.
/// </summary>
[Experimental("MEAI001")]
public sealed class McpServerToolApprovalResponseContent : UserInputResponseContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="McpServerToolApprovalResponseContent"/> class.
    /// </summary>
    /// <param name="requestId">The identifier of the <see cref="McpServerToolApprovalRequestContent"/> associated with this response.</param>
    /// <param name="approved"><see langword="true"/> if the MCP server tool call is approved; otherwise, <see langword="false"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="requestId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="requestId"/> is empty or composed entirely of whitespace.</exception>
    public McpServerToolApprovalResponseContent(string requestId, bool approved)
        : base(requestId)
    {
        Approved = approved;
    }

    /// <summary>
    /// Gets a value indicating whether the user approved the request.
    /// </summary>
    public bool Approved { get; }
}
