// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a request for user input.
/// </summary>
[Experimental("MEAI001")]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(FunctionApprovalRequestContent), "functionApprovalRequest")]
[JsonDerivedType(typeof(McpServerToolApprovalRequestContent), "mcpServerToolApprovalRequest")]
public class UserInputRequestContent : AIContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserInputRequestContent"/> class.
    /// </summary>
    /// <param name="requestId">The identifier of this request.</param>
    /// <exception cref="ArgumentNullException"><paramref name="requestId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="requestId"/> is empty or composed entirely of whitespace.</exception>
    protected UserInputRequestContent(string requestId)
    {
        RequestId = Throw.IfNullOrWhitespace(requestId);
    }

    /// <summary>
    /// Gets the identifier of this request.
    /// </summary>
    public string RequestId { get; }
}
