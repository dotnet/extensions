// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a request for user approval of an MCP server tool call.
/// </summary>
[Experimental("MEAI001")]
public sealed class McpServerToolApprovalRequestContent : UserInputRequestContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="McpServerToolApprovalRequestContent"/> class.
    /// </summary>
    /// <param name="id">The ID that uniquely identifies the MCP server tool approval request/response pair.</param>
    /// <param name="toolCall">The tool call that requires user approval.</param>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="id"/> is empty or composed entirely of whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="toolCall"/> is <see langword="null"/>.</exception>
    public McpServerToolApprovalRequestContent(string id, McpServerToolCallContent toolCall)
        : base(id)
    {
        ToolCall = Throw.IfNull(toolCall);
    }

    /// <summary>
    /// Gets the tool call that pre-invoke approval is required for.
    /// </summary>
    public McpServerToolCallContent ToolCall { get; }

    /// <summary>
    /// Creates a <see cref="McpServerToolApprovalResponseContent"/> to indicate whether the function call is approved or rejected based on the value of <paramref name="approved"/>.
    /// </summary>
    /// <param name="approved"><see langword="true"/> if the function call is approved; otherwise, <see langword="false"/>.</param>
    /// <returns>The <see cref="FunctionApprovalResponseContent"/> representing the approval response.</returns>
    public McpServerToolApprovalResponseContent CreateResponse(bool approved) => new(Id, approved);
}
