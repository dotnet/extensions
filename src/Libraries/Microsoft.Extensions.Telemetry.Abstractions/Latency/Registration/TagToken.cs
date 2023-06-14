// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Telemetry.Latency;

/// <summary>
/// Token representing a registered tag.
/// </summary>
[SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Comparing instances is not an expected scenario")]
public readonly struct TagToken
{
    /// <summary>
    /// Gets the name of the tag.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the position of the token in the token table.
    /// </summary>
    public int Position { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TagToken"/> struct.
    /// </summary>
    /// <param name="name">Name of the tag.</param>
    /// <param name="position">Position of the token in the token table.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    public TagToken(string name, int position)
    {
        Name = Throw.IfNullOrWhitespace(name);
        Position = position;
    }
}
