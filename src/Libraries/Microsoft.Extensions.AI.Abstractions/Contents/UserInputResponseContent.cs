﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents the response to a request for user input.
/// </summary>
[Experimental("MEAI001")]
public class UserInputResponseContent : AIContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserInputResponseContent"/> class.
    /// </summary>
    /// <param name="id">The ID that uniquely identifies the user input request/response pair.</param>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="id"/> is empty or composed entirely of whitespace.</exception>
    protected UserInputResponseContent(string id)
    {
        Id = Throw.IfNullOrWhitespace(id);
    }

    /// <summary>
    /// Gets the ID that uniquely identifies the user input request/response pair.
    /// </summary>
    public string Id { get; }
}
