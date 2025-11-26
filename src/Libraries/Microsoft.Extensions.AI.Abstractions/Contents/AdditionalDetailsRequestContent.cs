// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a request for additional details from the user.
/// </summary>
[Experimental("MEAI001")]
public sealed class AdditionalDetailsRequestContent : UserInputRequestContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdditionalDetailsRequestContent"/> class.
    /// </summary>
    /// <param name="id">The ID that uniquely identifies the additional details request/response pair.</param>
    /// <param name="request">The additional details request.</param>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="id"/> is empty or composed entirely of whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/>.</exception>
    public AdditionalDetailsRequestContent(string id, AIContent request)
        : base(id)
    {
        Request = Throw.IfNull(request);
    }

    /// <summary>
    /// Gets the additional details request.
    /// </summary>
    public AIContent Request { get; }

    /// <summary>
    /// Creates a <see cref="AdditionalDetailsResponseContent"/> to provide the requested additional details.
    /// </summary>
    /// <param name="response">The <see cref="AIContent"/> containing the requested additional details.</param>
    /// <returns>The <see cref="AdditionalDetailsResponseContent"/> representing the response.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="response"/> is <see langword="null"/>.</exception>
    public AdditionalDetailsResponseContent CreateResponse(AIContent response) => new(Id, response);
}
