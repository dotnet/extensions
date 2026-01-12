// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a request for user approval of a call content.
/// </summary>
[Experimental("MEAI001")]
public sealed class FunctionApprovalRequestContent : UserInputRequestContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionApprovalRequestContent"/> class.
    /// </summary>
    /// <param name="id">The ID that uniquely identifies the function approval request/response pair.</param>
    /// <param name="callContent">The call content that requires user approval.</param>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="id"/> is empty or composed entirely of whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="callContent"/> is <see langword="null"/>.</exception>
    public FunctionApprovalRequestContent(string id, AIContent callContent)
        : base(id)
    {
        CallContent = Throw.IfNull(callContent);
    }

    /// <summary>
    /// Gets the call content that pre-invoke approval is required for.
    /// </summary>
    public AIContent CallContent { get; }

    /// <summary>
    /// Creates a <see cref="FunctionApprovalResponseContent"/> to indicate whether the call is approved or rejected based on the value of <paramref name="approved"/>.
    /// </summary>
    /// <param name="approved"><see langword="true"/> if the call is approved; otherwise, <see langword="false"/>.</param>
    /// <param name="reason">An optional reason for the approval or rejection.</param>
    /// <returns>The <see cref="FunctionApprovalResponseContent"/> representing the approval response.</returns>
    public FunctionApprovalResponseContent CreateResponse(bool approved, string? reason = null) => new(Id, approved, CallContent) { Reason = reason };
}
