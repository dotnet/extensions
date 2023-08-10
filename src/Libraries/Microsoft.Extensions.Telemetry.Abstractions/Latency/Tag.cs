// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Telemetry.Latency;

/// <summary>
/// Represents a name and value pair to provide metadata about an operation being measured.
/// </summary>
[SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Comparing instances is not an expected scenario")]
public readonly struct Tag
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Tag"/> struct.
    /// </summary>
    /// <param name="name">Name of the tag.</param>
    /// <param name="value">Value of the tag.</param>
    public Tag(string name, string value)
    {
        Name = name;
        Value = value;
    }

    /// <summary>
    /// Gets the name of the tag.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the value of the tag.
    /// </summary>
    public string Value { get; }
}
