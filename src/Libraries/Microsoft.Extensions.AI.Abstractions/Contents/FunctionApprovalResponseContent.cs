// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a response to an approval request.
/// </summary>
[Experimental("MEAI001")]
public sealed class FunctionApprovalResponseContent : UserInputResponseContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionApprovalResponseContent"/> class.
    /// </summary>
    /// <param name="id">The ID that uniquely identifies the approval request/response pair.</param>
    /// <param name="approved"><see langword="true"/> if the call is approved; otherwise, <see langword="false"/>.</param>
    /// <param name="callContent">The call content that requires user approval.</param>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="id"/> is empty or composed entirely of whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="callContent"/> is <see langword="null"/>.</exception>
    public FunctionApprovalResponseContent(string id, bool approved, AIContent callContent)
        : base(id)
    {
        Approved = approved;
        CallContent = Throw.IfNull(callContent);
    }

    /// <summary>
    /// Gets a value indicating whether the user approved the request.
    /// </summary>
    public bool Approved { get; }

    /// <summary>
    /// Gets the call content for which approval was requested.
    /// </summary>
    public AIContent CallContent { get; }
}
