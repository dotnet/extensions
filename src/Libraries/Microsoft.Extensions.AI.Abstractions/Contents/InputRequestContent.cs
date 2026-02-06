// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a request for input from the user or application.
/// </summary>
[JsonDerivedType(typeof(FunctionApprovalRequestContent), "functionApprovalRequest")]
public class InputRequestContent : AIContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InputRequestContent"/> class.
    /// </summary>
    /// <param name="requestId">The unique identifier that correlates this request with its corresponding response.</param>
    /// <exception cref="ArgumentNullException"><paramref name="requestId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="requestId"/> is empty or composed entirely of whitespace.</exception>
    protected InputRequestContent(string requestId)
    {
        RequestId = Throw.IfNullOrWhitespace(requestId);
    }

    /// <summary>
    /// Gets the unique identifier that correlates this request with its corresponding <see cref="InputResponseContent"/>.
    /// </summary>
    public string RequestId { get; }
}
