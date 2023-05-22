// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Compliance.Redaction;

/// <summary>
/// No-op redactor implementation used for data classes that don't require any redaction.
/// </summary>
internal sealed class NullRedactor : Redactor
{
    private NullRedactor()
    {
    }

    /// <summary>
    /// Gets the singleton instance of this class.
    /// </summary>
    public static NullRedactor Instance { get; } = new();

    /// <inheritdoc/>
    public override int GetRedactedLength(ReadOnlySpan<char> input) => input.Length;

    /// <inheritdoc/>
    public override int Redact(ReadOnlySpan<char> source, Span<char> destination)
    {
        Throw.IfBufferTooSmall(destination.Length, source.Length);

        // span.CopyTo method throws on 0 input, it is not an error in this case so we can just return.
        if (source.IsEmpty)
        {
            return 0;
        }

        source.CopyTo(destination);

        return source.Length;
    }
}
