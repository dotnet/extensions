// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a request for user approval of a function call.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AIFunctionApprovals, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class FunctionApprovalRequestContent : InputRequestContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionApprovalRequestContent"/> class.
    /// </summary>
    /// <param name="requestId">The identifier of this request.</param>    /// <param name="functionCall">The function call that requires user approval.</param>
    /// <exception cref="ArgumentNullException"><paramref name="requestId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="requestId"/> is empty or composed entirely of whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="functionCall"/> is <see langword="null"/>.</exception>
    public FunctionApprovalRequestContent(string requestId, FunctionCallContent functionCall)
        : base(requestId)
    {
        FunctionCall = Throw.IfNull(functionCall);
    }

    /// <summary>
    /// Gets the function call that pre-invoke approval is required for.
    /// </summary>
    public FunctionCallContent FunctionCall { get; }

    /// <summary>
    /// Creates a <see cref="FunctionApprovalResponseContent"/> to indicate whether the function call is approved or rejected based on the value of <paramref name="approved"/>.
    /// </summary>
    /// <param name="approved"><see langword="true"/> if the function call is approved; otherwise, <see langword="false"/>.</param>
    /// <param name="reason">An optional reason for the approval or rejection.</param>
    /// <returns>The <see cref="FunctionApprovalResponseContent"/> representing the approval response.</returns>
    public FunctionApprovalResponseContent CreateResponse(bool approved, string? reason = null) => new(RequestId, approved, FunctionCall) { Reason = reason };
}
