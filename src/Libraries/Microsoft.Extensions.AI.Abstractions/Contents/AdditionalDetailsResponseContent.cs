// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a response for additional details request from the user.
/// </summary>
[Experimental("MEAI001")]
public sealed class AdditionalDetailsResponseContent : UserInputResponseContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdditionalDetailsResponseContent"/> class.
    /// </summary>
    /// <param name="id">The ID that uniquely identifies the additional details request/response pair.</param>
    /// <param name="response">The additional details response.</param>
    public AdditionalDetailsResponseContent(string id, AIContent response)
        : base(id)
    {
        Response = Throw.IfNull(response);
    }

    /// <summary>
    /// Gets the additional details response.
    /// </summary>
    public AIContent Response { get; }
}
