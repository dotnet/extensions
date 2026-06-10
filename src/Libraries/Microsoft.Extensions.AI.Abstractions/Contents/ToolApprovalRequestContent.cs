// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.DiagnosticIds;
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
    /// Gets or sets a value indicating whether this approval request needs a confirmation
    /// from the consumer before the underlying tool call is invoked.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Defaults to <see langword="true"/>, indicating that the underlying tool genuinely
    /// requires approval (for example, the targeted function is an <see cref="ApprovalRequiredAIFunction"/>)
    /// and the consumer must obtain a confirmation (for instance, a user prompt, a policy decision,
    /// or a governance gate) before the call is invoked.
    /// </para>
    /// <para>
    /// Some invokers convert every concurrent function call in a response into a
    /// <see cref="ToolApprovalRequestContent"/> whenever any one of them targets an
    /// <see cref="ApprovalRequiredAIFunction"/>, so that approvals and rejections stay coherent
    /// across the response. When set to <see langword="false"/>, this property indicates that
    /// the underlying tool did not itself require approval and that the request exists only to
    /// satisfy that constraint; consumers may auto-approve such requests without seeking a
    /// confirmation.
    /// </para>
    /// </remarks>
    [Experimental(DiagnosticIds.Experiments.AIFunctionApprovals, UrlFormat = DiagnosticIds.UrlFormat)]
    public bool RequiresConfirmation { get; set; } = true;

    /// <summary>
    /// Creates a <see cref="ToolApprovalResponseContent"/> indicating whether the tool call is approved or rejected.
    /// </summary>
    /// <param name="approved"><see langword="true"/> if the tool call is approved; otherwise, <see langword="false"/>.</param>
    /// <param name="reason">An optional reason for the approval or rejection.</param>
    /// <returns>The <see cref="ToolApprovalResponseContent"/> correlated with this request.</returns>
    public ToolApprovalResponseContent CreateResponse(bool approved, string? reason = null) =>
        new ToolApprovalResponseContent(RequestId, approved, ToolCall) { Reason = reason };
}
