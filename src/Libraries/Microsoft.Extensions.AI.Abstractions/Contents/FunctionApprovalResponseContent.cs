// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a response to a <see cref="FunctionApprovalRequestContent"/>, indicating whether the function call was approved.
/// </summary>
public sealed class FunctionApprovalResponseContent : InputResponseContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionApprovalResponseContent"/> class.
    /// </summary>
    /// <param name="requestId">The unique identifier of the <see cref="FunctionApprovalRequestContent"/> associated with this response.</param>
    /// <param name="approved"><see langword="true"/> if the function call is approved; otherwise, <see langword="false"/>.</param>
    /// <param name="functionCall">The function call that was subject to approval.</param>
    /// <exception cref="ArgumentNullException"><paramref name="requestId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="requestId"/> is empty or composed entirely of whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="functionCall"/> is <see langword="null"/>.</exception>
    public FunctionApprovalResponseContent(string requestId, bool approved, FunctionCallContent functionCall)
        : base(requestId)
    {
        Approved = approved;
        FunctionCall = Throw.IfNull(functionCall);
    }

    /// <summary>
    /// Gets a value indicating whether the function call was approved for execution.
    /// </summary>
    public bool Approved { get; }

    /// <summary>
    /// Gets the function call that was subject to approval.
    /// </summary>
    public FunctionCallContent FunctionCall { get; }

    /// <summary>
    /// Gets or sets the optional reason for the approval or rejection.
    /// </summary>
    public string? Reason { get; set; }
}
