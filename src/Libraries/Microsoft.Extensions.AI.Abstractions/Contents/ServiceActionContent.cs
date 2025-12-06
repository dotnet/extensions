// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents an action performed by a hosted service.
/// </summary>
/// <remarks>
/// This content type is used to represent actions performed by the service such as calls to other services or invocation of service tools.
/// It is informational only.
/// </remarks>
[Experimental("MEAI001")]
public class ServiceActionContent : AIContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceActionContent"/> class.
    /// </summary>
    /// <param name="id">The ID for the service-side action.</param>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="id"/> is empty or composed entirely of whitespace.</exception>
    public ServiceActionContent(string id)
    {
        Id = Throw.IfNullOrWhitespace(id);
    }

    /// <summary>
    /// Gets the ID for the service-side action.
    /// </summary>
    public string Id { get; }
}
