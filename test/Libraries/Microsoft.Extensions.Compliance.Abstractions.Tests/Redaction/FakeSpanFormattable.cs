// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET6_0_OR_GREATER

using System;

namespace Microsoft.Extensions.Compliance.Redaction.Tests;

internal sealed class FakeSpanFormattable : ISpanFormattable
{
    private readonly string _value;

    public FakeSpanFormattable(string value)
    {
        _value = value;
    }

    public string ToString(string? format, System.IFormatProvider? formatProvider)
    {
        return _value;
    }

    public bool TryFormat(System.Span<char> destination, out int charsWritten,
        System.ReadOnlySpan<char> format, System.IFormatProvider? provider)
    {
        if (destination.Length < _value.Length)
        {
            charsWritten = 0;
            return false;
        }

        for (var i = 0; i < _value.Length; i++)
        {
            destination[i] = _value[i];
        }

        charsWritten = _value.Length;

        return true;
    }
}

#endif
