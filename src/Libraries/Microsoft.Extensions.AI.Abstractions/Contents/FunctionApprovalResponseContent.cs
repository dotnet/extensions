// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a response to a function approval request.
/// </summary>
[Experimental("MEAI001")]
public sealed class FunctionApprovalResponseContent : UserInputResponseContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionApprovalResponseContent"/> class.
    /// </summary>
    /// <param name="id">The ID to uniquely identify the function approval request/response pair.</param>
    /// <param name="approved">Indicates whether the request was approved.</param>
    /// <param name="functionCall">The function call that requires user approval.</param>
    public FunctionApprovalResponseContent(string id, bool approved, FunctionCallContent functionCall)
        : base(id)
    {
        Approved = approved;
        FunctionCall = Throw.IfNull(functionCall);
    }

    /// <summary>
    /// Gets a value indicating whether the user approved the request.
    /// </summary>
    public bool Approved { get; }

    /// <summary>
    /// Gets the function call that pre-invoke approval is required for.
    /// </summary>
    public FunctionCallContent FunctionCall { get; }
}
