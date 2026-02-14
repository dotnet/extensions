// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a response to a <see cref="ToolApprovalRequestContent"/>, indicating whether the tool call was approved.
/// </summary>
public sealed class ToolApprovalResponseContent : InputResponseContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToolApprovalResponseContent"/> class.
    /// </summary>
    /// <param name="requestId">The unique identifier of the <see cref="ToolApprovalRequestContent"/> associated with this response.</param>
    /// <param name="approved"><see langword="true"/> if the tool call is approved; otherwise, <see langword="false"/>.</param>
    /// <param name="functionCall">The function call that was subject to approval.</param>
    /// <exception cref="ArgumentNullException"><paramref name="requestId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="requestId"/> is empty or composed entirely of whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="functionCall"/> is <see langword="null"/>.</exception>
    public ToolApprovalResponseContent(string requestId, bool approved, FunctionCallContent functionCall)
        : base(requestId)
    {
        Approved = approved;
        ToolCall = Throw.IfNull(functionCall);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolApprovalResponseContent"/> class.
    /// </summary>
    /// <param name="requestId">The unique identifier of the <see cref="ToolApprovalRequestContent"/> associated with this response.</param>
    /// <param name="approved"><see langword="true"/> if the tool call is approved; otherwise, <see langword="false"/>.</param>
    /// <param name="mcpServerToolCall">The MCP server tool call that was subject to approval.</param>
    /// <exception cref="ArgumentNullException"><paramref name="requestId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="requestId"/> is empty or composed entirely of whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="mcpServerToolCall"/> is <see langword="null"/>.</exception>
    public ToolApprovalResponseContent(string requestId, bool approved, McpServerToolCallContent mcpServerToolCall)
        : base(requestId)
    {
        Approved = approved;
        ToolCall = Throw.IfNull(mcpServerToolCall);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolApprovalResponseContent"/> class for JSON deserialization.
    /// </summary>
    /// <param name="requestId">The unique identifier of the <see cref="ToolApprovalRequestContent"/> associated with this response.</param>
    /// <param name="approved"><see langword="true"/> if the tool call is approved; otherwise, <see langword="false"/>.</param>
    /// <param name="toolCall">The tool call that was subject to approval.</param>
    [JsonConstructor]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ToolApprovalResponseContent(string requestId, bool approved, ToolCallContent toolCall)
        : base(requestId)
    {
        _ = Throw.IfNull(toolCall);

        if (toolCall is not FunctionCallContent and not McpServerToolCallContent)
        {
            Throw.ArgumentException(nameof(toolCall), $"Unsupported type '{toolCall.GetType().Name}'.");
        }

        Approved = approved;
        ToolCall = toolCall;
    }

    /// <summary>
    /// Gets a value indicating whether the tool call was approved for execution.
    /// </summary>
    public bool Approved { get; }

    /// <summary>
    /// Gets the tool call that was subject to approval.
    /// </summary>
    public ToolCallContent ToolCall { get; }

    /// <summary>
    /// Gets or sets the optional reason for the approval or rejection.
    /// </summary>
    public string? Reason { get; set; }
}
