// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents the response to a request for user input.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(FunctionApprovalResponseContent), "functionApprovalResponse")]
public class InputResponseContent : AIContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InputResponseContent"/> class.
    /// </summary>
    /// <param name="requestId">The ID that uniquely identifies the user input request/response pair.</param>
    /// <exception cref="ArgumentNullException"><paramref name="requestId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="requestId"/> is empty or composed entirely of whitespace.</exception>
    protected InputResponseContent(string requestId)
    {
        RequestId = Throw.IfNullOrWhitespace(requestId);
    }

    /// <summary>
    /// Gets the ID that uniquely identifies the user input request/response pair.
    /// </summary>
    public string RequestId { get; }
}