// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents the response to a request for user input.
/// </summary>
[Experimental("MEAI001")]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(FunctionApprovalResponseContent), "functionApprovalResponse")]
[JsonDerivedType(typeof(McpServerToolApprovalResponseContent), "mcpServerToolApprovalResponse")]
public class UserInputResponseContent : AIContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserInputResponseContent"/> class.
    /// </summary>
    /// <param name="requestId">The identifier of the <see cref="UserInputRequestContent"/> associated with this response.</param>
    /// <exception cref="ArgumentNullException"><paramref name="requestId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="requestId"/> is empty or composed entirely of whitespace.</exception>
    protected UserInputResponseContent(string requestId)
    {
        RequestId = Throw.IfNullOrWhitespace(requestId);
    }

    /// <summary>
    /// Gets the identifier of the <see cref="UserInputRequestContent"/> associated with this response.
    /// </summary>
    public string RequestId { get; }
}
