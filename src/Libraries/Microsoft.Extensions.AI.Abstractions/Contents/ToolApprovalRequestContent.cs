// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
    /// <param name="requestId">The unique identifier that correlates this request with its corresponding response.</param>
    /// <param name="toolCall">The tool call that requires approval before execution.</param>
    /// <exception cref="ArgumentNullException"><paramref name="requestId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="requestId"/> is empty or composed entirely of whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="toolCall"/> is <see langword="null"/>.</exception>
    [JsonConstructor]
    public ToolApprovalRequestContent(string requestId, ToolCallContent toolCall)
        : base(requestId)
    {
        ToolCall = Throw.IfNull(toolCall);
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
    public ToolApprovalResponseContent CreateResponse(bool approved, string? reason = null) =>
        new ToolApprovalResponseContent(RequestId, approved, ToolCall) { Reason = reason };
}
