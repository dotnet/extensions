// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Compliance.Redaction;

internal sealed class BlottingRedactor : Redactor
{
    private readonly char _blottingCharacter;

    public BlottingRedactor(IOptions<BlottingRedactorOptions> options)
    {
        _blottingCharacter = options.Value.BlottingCharacter;
    }

    public override int GetRedactedLength(ReadOnlySpan<char> input) => input.Length;

    public override int Redact(ReadOnlySpan<char> source, Span<char> destination)
    {
        if (source.Length == 0)
        {
            return 0;
        }

        Throw.IfBufferTooSmall(destination.Length, source.Length, nameof(destination));

        destination.Fill(_blottingCharacter);
        return source.Length;
    }
}
