// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Compliance.Redaction;

/// <summary>
/// Redactor that does nothing to its input and returns it as-is.
/// </summary>
public sealed class NullRedactor : Redactor
{
    /// <summary>
    /// Gets the singleton instance of this class.
    /// </summary>
    public static NullRedactor Instance { get; } = new();

    /// <inheritdoc/>
    public override int GetRedactedLength(ReadOnlySpan<char> input) => input.Length;

    /// <inheritdoc/>
    public override int Redact(ReadOnlySpan<char> source, Span<char> destination)
    {
        if (!source.TryCopyTo(destination))
        {
            // will throw unconditionally, with a nice error message
            Throw.IfBufferTooSmall(destination.Length, source.Length, nameof(destination));
        }

        return source.Length;
    }

    /// <inheritdoc/>
    public override string Redact(string? source) => source ?? string.Empty;
}
