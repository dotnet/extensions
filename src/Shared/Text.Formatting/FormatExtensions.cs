// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NETCOREAPP3_1_OR_GREATER
using System;
using System.Globalization;

#pragma warning disable CA1716
namespace Microsoft.Shared.Text;
#pragma warning restore CA1716

#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
internal static class FormatExtensions
{
    public static bool TryFormat(this DateTime value, Span<char> target, out int charsWritten, string? format, IFormatProvider? provider)
    {
        var s = value.ToString(format, provider);
        if (s.Length > target.Length)
        {
            charsWritten = 0;
            return false;
        }

        charsWritten = s.Length;
        for (int i = 0; i < s.Length; i++)
        {
            target[i] = s[i];
        }

        return true;
    }

    public static bool TryFormat(this TimeSpan value, Span<char> target, out int charsWritten, string? format, IFormatProvider? provider)
    {
        var s = value.ToString(format, provider);
        if (s.Length > target.Length)
        {
            charsWritten = 0;
            return false;
        }

        charsWritten = s.Length;
        for (int i = 0; i < s.Length; i++)
        {
            target[i] = s[i];
        }

        return true;
    }

    public static bool TryFormat(this long value, Span<char> target, out int charsWritten, string? format, IFormatProvider? provider)
    {
        var s = value.ToString(format, provider);
        if (s.Length > target.Length)
        {
            charsWritten = 0;
            return false;
        }

        charsWritten = s.Length;
        for (int i = 0; i < s.Length; i++)
        {
            target[i] = s[i];
        }

        return true;
    }

    public static bool TryFormat(this double value, Span<char> target, out int charsWritten, string? format, IFormatProvider? provider)
    {
        var s = value.ToString(format, provider);
        if (s.Length > target.Length)
        {
            charsWritten = 0;
            return false;
        }

        charsWritten = s.Length;
        for (int i = 0; i < s.Length; i++)
        {
            target[i] = s[i];
        }

        return true;
    }

    public static bool TryFormat(this decimal value, Span<char> target, out int charsWritten, string? format, IFormatProvider? provider)
    {
        var s = value.ToString(format, provider);
        if (s.Length > target.Length)
        {
            charsWritten = 0;
            return false;
        }

        charsWritten = s.Length;
        for (int i = 0; i < s.Length; i++)
        {
            target[i] = s[i];
        }

        return true;
    }

    public static bool TryFormat(this ulong value, Span<char> target, out int charsWritten, string? format, IFormatProvider? provider)
    {
        var s = value.ToString(format, provider);
        if (s.Length > target.Length)
        {
            charsWritten = 0;
            return false;
        }

        charsWritten = s.Length;
        for (int i = 0; i < s.Length; i++)
        {
            target[i] = s[i];
        }

        return true;
    }

    public static bool TryFormat(this bool value, Span<char> target, out int charsWritten)
    {
        var s = value.ToString(CultureInfo.InvariantCulture);
        if (s.Length > target.Length)
        {
            charsWritten = 0;
            return false;
        }

        charsWritten = s.Length;
        for (int i = 0; i < s.Length; i++)
        {
            target[i] = s[i];
        }

        return true;
    }
}
#endif
