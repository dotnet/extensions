// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a request for approval before invoking a function call.
/// </summary>
public sealed class FunctionApprovalRequestContent : InputRequestContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionApprovalRequestContent"/> class.
    /// </summary>
    /// <param name="requestId">The unique identifier that correlates this request with its corresponding response. This is typically not the same as the function call ID.</param>
    /// <param name="functionCall">The function call that requires approval before execution.</param>
    /// <exception cref="ArgumentNullException"><paramref name="requestId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="requestId"/> is empty or composed entirely of whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="functionCall"/> is <see langword="null"/>.</exception>
    public FunctionApprovalRequestContent(string requestId, FunctionCallContent functionCall)
        : base(requestId)
    {
        FunctionCall = Throw.IfNull(functionCall);
    }

    /// <summary>
    /// Gets the function call that requires approval before execution.
    /// </summary>
    public FunctionCallContent FunctionCall { get; }

    /// <summary>
    /// Creates a <see cref="FunctionApprovalResponseContent"/> indicating whether the function call is approved or rejected.
    /// </summary>
    /// <param name="approved"><see langword="true"/> if the function call is approved; otherwise, <see langword="false"/>.</param>
    /// <param name="reason">An optional reason for the approval or rejection.</param>
    /// <returns>The <see cref="FunctionApprovalResponseContent"/> correlated with this request.</returns>
    public FunctionApprovalResponseContent CreateResponse(bool approved, string? reason = null) => new(RequestId, approved, FunctionCall) { Reason = reason };
}
