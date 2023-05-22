// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Compliance.Redaction;

/// <summary>
/// Redactor that replaces anything with an empty string.
/// </summary>
public sealed class ErasingRedactor : Redactor
{
    /// <summary>
    /// Gets the singleton instance of <see cref="ErasingRedactor"/>.
    /// </summary>
    public static ErasingRedactor Instance { get; } = new();

    /// <inheritdoc/>
    public override int Redact(ReadOnlySpan<char> source, Span<char> destination) => 0;

    /// <inheritdoc/>
    public override int GetRedactedLength(ReadOnlySpan<char> input) => 0;
}
