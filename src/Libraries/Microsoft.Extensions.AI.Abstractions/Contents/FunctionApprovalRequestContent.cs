// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a request for user approval of a function call.
/// </summary>
[Experimental("MEAI001")]
public sealed class FunctionApprovalRequestContent : UserInputRequestContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionApprovalRequestContent"/> class.
    /// </summary>
    /// <param name="id">The ID to uniquely identify the function approval request/response pair.</param>
    /// <param name="functionCall">The function call that requires user approval.</param>
    public FunctionApprovalRequestContent(string id, FunctionCallContent functionCall)
        : base(id)
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
    /// <returns>The <see cref="FunctionApprovalResponseContent"/> representing the approval response.</returns>
    public FunctionApprovalResponseContent CreateResponse(bool approved)
        => new(Id, approved, FunctionCall);
}
