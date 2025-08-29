// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Base class for user input response content.
/// </summary>
[Experimental("MEAI001")]
public class UserInputResponseContent : AIContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserInputResponseContent"/> class.
    /// </summary>
    /// <param name="id">The ID to uniquely identify the user input request/response pair.</param>
    protected UserInputResponseContent(string id)
    {
        Id = Throw.IfNullOrWhitespace(id);
    }

    /// <summary>
    /// Gets the ID to uniquely identify the user input request/response pair.
    /// </summary>
    public string Id { get; }
}
