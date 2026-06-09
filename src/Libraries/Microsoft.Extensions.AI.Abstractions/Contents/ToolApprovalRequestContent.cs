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
    /// Gets or sets a value indicating whether this approval request was added by the invoker
    /// of the tool call rather than because the underlying tool itself was declared as requiring approval.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Defaults to <see langword="false"/>, indicating that the underlying tool itself required
    /// approval (for example, the targeted function was an <see cref="ApprovalRequiredAIFunction"/>).
    /// </para>
    /// <para>
    /// Some invokers convert every concurrent function call in a response into a
    /// <see cref="ToolApprovalRequestContent"/> whenever any one of them targets an
    /// <see cref="ApprovalRequiredAIFunction"/>, so that approvals and rejections stay coherent
    /// across the batch. When set to <see langword="true"/>, this property indicates that the
    /// request was added for that reason and the underlying tool did not itself require approval;
    /// consumers may use this to, for example, auto-approve such requests.
    /// </para>
    /// </remarks>
    [Experimental(DiagnosticIds.Experiments.AIFunctionApprovals, UrlFormat = DiagnosticIds.UrlFormat)]
    public bool IsInvokerRequested { get; set; }

    /// <summary>
    /// Creates a <see cref="ToolApprovalResponseContent"/> indicating whether the tool call is approved or rejected.
    /// </summary>
    /// <param name="approved"><see langword="true"/> if the tool call is approved; otherwise, <see langword="false"/>.</param>
    /// <param name="reason">An optional reason for the approval or rejection.</param>
    /// <returns>The <see cref="ToolApprovalResponseContent"/> correlated with this request.</returns>
    public ToolApprovalResponseContent CreateResponse(bool approved, string? reason = null) =>
        new ToolApprovalResponseContent(RequestId, approved, ToolCall) { Reason = reason };
}
