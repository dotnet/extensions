// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a request for approval before invoking a tool call.
/// </summary>
public sealed class ToolApprovalRequestContent : InputRequestContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToolApprovalRequestContent"/> class.
    /// </summary>
    /// <param name="requestId">The unique identifier that correlates this request with its corresponding response. This may differ from the <see cref="ToolCallContent.CallId"/> of the specified <paramref name="functionCall"/>.</param>
    /// <param name="functionCall">The function call that requires approval before execution.</param>
    /// <exception cref="ArgumentNullException"><paramref name="requestId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="requestId"/> is empty or composed entirely of whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="functionCall"/> is <see langword="null"/>.</exception>
    public ToolApprovalRequestContent(string requestId, FunctionCallContent functionCall)
        : base(requestId)
    {
        ToolCall = Throw.IfNull(functionCall);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolApprovalRequestContent"/> class.
    /// </summary>
    /// <param name="requestId">The unique identifier that correlates this request with its corresponding response. This may differ from the <see cref="ToolCallContent.CallId"/> of the specified <paramref name="mcpServerToolCall"/>.</param>
    /// <param name="mcpServerToolCall">The MCP server tool call that requires approval before execution.</param>
    /// <exception cref="ArgumentNullException"><paramref name="requestId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="requestId"/> is empty or composed entirely of whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="mcpServerToolCall"/> is <see langword="null"/>.</exception>
    public ToolApprovalRequestContent(string requestId, McpServerToolCallContent mcpServerToolCall)
        : base(requestId)
    {
        ToolCall = Throw.IfNull(mcpServerToolCall);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolApprovalRequestContent"/> class for JSON deserialization.
    /// </summary>
    /// <param name="requestId">The unique identifier that correlates this request with its corresponding response.</param>
    /// <param name="toolCall">The tool call that requires approval before execution.</param>
    [JsonConstructor]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ToolApprovalRequestContent(string requestId, ToolCallContent toolCall)
        : base(requestId)
    {
        _ = Throw.IfNull(toolCall);

        if (toolCall is not FunctionCallContent and not McpServerToolCallContent)
        {
            Throw.ArgumentException(nameof(toolCall), $"Unsupported type '{toolCall.GetType().Name}'.");
        }

        ToolCall = toolCall;
    }

    /// <summary>
    /// Gets the tool call that requires approval before execution.
    /// </summary>
    public ToolCallContent ToolCall { get; }

    /// <summary>
    /// Creates a <see cref="ToolApprovalResponseContent"/> indicating whether the tool call is approved or rejected.
    /// </summary>
    /// <param name="approved"><see langword="true"/> if the tool call is approved; otherwise, <see langword="false"/>.</param>
    /// <param name="reason">An optional reason for the approval or rejection.</param>
    /// <returns>The <see cref="ToolApprovalResponseContent"/> correlated with this request.</returns>
    public ToolApprovalResponseContent CreateResponse(bool approved, string? reason = null) => ToolCall switch
    {
        FunctionCallContent fcc => new ToolApprovalResponseContent(RequestId, approved, fcc) { Reason = reason },
        McpServerToolCallContent mcp => new ToolApprovalResponseContent(RequestId, approved, mcp) { Reason = reason },

        // This should never occur since the constructor enforces the allowed types.
        _ => throw new InvalidOperationException($"Unsupported ToolCallContent type '{ToolCall.GetType().Name}'."),
    };
}
