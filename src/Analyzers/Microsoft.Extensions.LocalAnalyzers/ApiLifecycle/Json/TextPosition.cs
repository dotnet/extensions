// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Forked from StyleCop.Analyzers repo.

namespace Microsoft.Extensions.LocalAnalyzers.Json;

/// <summary>
/// Represents a position within a plain text resource.
/// </summary>
#pragma warning disable CA1815 // Override equals and operator equals on value types
public readonly struct TextPosition
#pragma warning restore CA1815 // Override equals and operator equals on value types
{
    /// <summary>
    /// Gets the column position, 0-based.
    /// </summary>
    public long Column { get; }

    /// <summary>
    /// Gets the line position, 0-based.
    /// </summary>
    public long Line { get; }

    public TextPosition(long column, long line)
    {
        Column = column;
        Line = line;
    }
}
