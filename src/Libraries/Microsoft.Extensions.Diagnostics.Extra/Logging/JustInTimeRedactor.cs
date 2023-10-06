// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Pools;

#if !NET6_0_OR_GREATER
using Microsoft.Shared.Text;
#endif

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Performs delayed redaction in order to avoid allocating intermediate strings and redact from span to span.
/// </summary>
internal sealed class JustInTimeRedactor : IResettable
#if NET6_0_OR_GREATER
    , ISpanFormattable
#else
    , IFormattable
#endif
{
    public static JustInTimeRedactor Get() => _pool.Get();
    public void Return() => _pool.Return(this);

    public JustInTimeRedactor? Next { get; set; }
    public Redactor? Redactor { get; set; }
    public string Salt { get; set; } = string.Empty;
    public bool AddDayOfYearToSalt { get; set; }
    public object? Value { get; set; }
    public override string ToString() => Redactor!.Redact(Value, null, CultureInfo.InvariantCulture);
    public string ToString(string? format, IFormatProvider? formatProvider) => Redactor!.Redact(Value, format, formatProvider);

    internal static TimeProvider Clock { get; set; } = TimeProvider.System;

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (Salt.Length > 0)
        {
            return TryRedactWithSalt(Value, destination, out charsWritten, format, provider);
        }

        return Redactor!.TryRedact(Value, destination, out charsWritten, format, provider);
    }

    public bool TryReset()
    {
        Value = null;
        return true;
    }

    private const int MaxStackAllocation = 256;
    private const int MaxDayOfYearLength = 3;

    [SkipLocalsInit]
    private bool TryRedactWithSalt(object? value, Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider = null)
    {
        Span<char> buffer = stackalloc char[MaxStackAllocation];
        int charsInBuffer = 0;
        char[]? a = null;

#if NET6_0_OR_GREATER
        bool done = false;
        if (value is ISpanFormattable sf)
        {
            done = sf.TryFormat(buffer, out charsInBuffer, format, provider);
        }

        if (done)
        {
            int maxLen = charsInBuffer + Salt.Length + MaxDayOfYearLength;
            if (maxLen > buffer.Length)
            {
                a = ArrayPool<char>.Shared.Rent(maxLen);
                buffer.CopyTo(a);
                buffer = a;
            }
        }
        else
#endif
#pragma warning disable S1199
        {
#pragma warning restore S1199
            ReadOnlySpan<char> payload = default;
            if (value is IFormattable f)
            {
                payload = f.ToString(format.Length > 0 ? format.ToString() : string.Empty, provider).AsSpan();
            }
            else if (value is char[] c)
            {
                // An attempt to call value.ToString() on a char[] will produce the string "System.Char[]" and redaction will be attempted on it,
                // instead of the provided array.
                payload = c.AsSpan();
            }
            else
            {
                var str = value?.ToString() ?? string.Empty;
                payload = str.AsSpan();
            }

            int maxLen = payload.Length + Salt.Length + MaxDayOfYearLength;
            if (maxLen > buffer.Length)
            {
                a = ArrayPool<char>.Shared.Rent(maxLen);
                buffer = a;
            }

            payload.CopyTo(buffer);
            charsInBuffer = payload.Length;
        }

        Salt.AsSpan().CopyTo(buffer.Slice(charsInBuffer));
        charsInBuffer += Salt.Length;

        if (AddDayOfYearToSalt)
        {
#if NET6_0_OR_GREATER
            _ = Clock.GetUtcNow().DayOfYear.TryFormat(buffer.Slice(charsInBuffer), out int doyLen, null, CultureInfo.InvariantCulture);
#else
            var doy = Clock.GetUtcNow().DayOfYear.ToInvariantString();
            doy.AsSpan().CopyTo(buffer.Slice(charsInBuffer));
            int doyLen = doy.Length;
#endif

            charsInBuffer -= MaxDayOfYearLength;
            charsInBuffer += doyLen;
        }

        var final = buffer.Slice(charsInBuffer);

        int len = Redactor!.GetRedactedLength(final);
        if (len > destination.Length)
        {
            charsWritten = 0;

            if (a != null)
            {
                ArrayPool<char>.Shared.Return(a);
            }

            return false;
        }

        charsWritten = Redactor!.Redact(final, destination);

        if (a != null)
        {
            ArrayPool<char>.Shared.Return(a);
        }

        return true;
    }

    private static readonly ObjectPool<JustInTimeRedactor> _pool = PoolFactory.CreatePool<JustInTimeRedactor>();
}
