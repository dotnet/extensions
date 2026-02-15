// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents the response to an <see cref="InputRequestContent"/>.
/// </summary>
[JsonDerivedType(typeof(FunctionApprovalResponseContent), "functionApprovalResponse")]
public class InputResponseContent : AIContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InputResponseContent"/> class.
    /// </summary>
    /// <param name="requestId">The unique identifier that correlates this response with its corresponding request.</param>
    /// <exception cref="ArgumentNullException"><paramref name="requestId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="requestId"/> is empty or composed entirely of whitespace.</exception>
    protected InputResponseContent(string requestId)
    {
        RequestId = Throw.IfNullOrWhitespace(requestId);
    }

    /// <summary>
    /// Gets the unique identifier that correlates this response with its corresponding <see cref="InputRequestContent"/>.
    /// </summary>
    public string RequestId { get; }
}
