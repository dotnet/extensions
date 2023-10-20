// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Performs delayed redaction in order to avoid allocating intermediate strings and enabling redacting from span to span when possible.
/// </summary>
internal sealed class JustInTimeRedactor : IResettable
#if NET6_0_OR_GREATER
    , ISpanFormattable
#else
    , IFormattable
#endif
{
    private const int MaxStackAllocation = 256;
    private const int MaxDayOfYearLength = 3;
    private const string DiscriminatorSeparator = ":";

    private Redactor? _redactor;
    private string _discriminator = string.Empty;
    private object? _value;

    public static JustInTimeRedactor Get(object? value, Redactor redactor, string discriminator)
    {
        var jr = _pool.Get();

        jr._value = value;
        jr._redactor = redactor;
        jr._discriminator = discriminator;

        return jr;
    }

    public void Return() => _pool.Return(this);

    public JustInTimeRedactor? Next { get; set; }

    public override string ToString() => ToString(null, CultureInfo.InvariantCulture);

    [SkipLocalsInit]
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        if (_discriminator.Length == 0)
        {
            return _redactor!.Redact(_value, format, formatProvider);
        }

        _ = TryRedactWithDiscriminator(_value, [], out int _, format.AsSpan(), formatProvider, out var result, generateResult: true);
        return result;
    }

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_discriminator.Length == 0)
        {
            return _redactor!.TryRedact(_value, destination, out charsWritten, format, provider);
        }

        return TryRedactWithDiscriminator(_value, destination, out charsWritten, format, provider, out string _, generateResult: false);
    }

    bool IResettable.TryReset()
    {
        _value = null;
        _redactor = null;
        _discriminator = string.Empty;
        return true;
    }

    [SkipLocalsInit]
    private bool TryRedactWithDiscriminator(object? value, Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? formatProvider,
        out string result, bool generateResult)
    {
        Span<char> workBuffer = stackalloc char[MaxStackAllocation];
        int charsInWorkBuffer = 0;
        char[]? a = null;

        // first step is to acquire the actual text to be redacted

#if NET6_0_OR_GREATER
        bool done = false;
        if (value is ISpanFormattable sf)
        {
            done = sf.TryFormat(workBuffer, out charsInWorkBuffer, format, formatProvider);
        }

        if (done)
        {
            int maxLenToRedact = charsInWorkBuffer + DiscriminatorSeparator.Length + _discriminator.Length + MaxDayOfYearLength;
            if (maxLenToRedact > workBuffer.Length)
            {
                a = ArrayPool<char>.Shared.Rent(maxLenToRedact);
                workBuffer.Slice(0, charsInWorkBuffer).CopyTo(a);
                workBuffer = a;
            }
        }
        else
#endif
#pragma warning disable S1199
        {
#pragma warning restore S1199
            ReadOnlySpan<char> inputAsSpan = default;
            if (value is IFormattable f)
            {
                inputAsSpan = f.ToString(format.Length > 0 ? format.ToString() : string.Empty, formatProvider).AsSpan();
            }
            else if (value is char[] c)
            {
                // An attempt to call value.ToString() on a char[] will produce the string "System.Char[]" and redaction will be attempted on it,
                // instead of the provided array.
                inputAsSpan = c.AsSpan();
            }
            else
            {
                var str = value?.ToString() ?? string.Empty;
                inputAsSpan = str.AsSpan();
            }

            int maxLenToRedact = inputAsSpan.Length + DiscriminatorSeparator.Length + _discriminator.Length + MaxDayOfYearLength;
            if (maxLenToRedact > workBuffer.Length)
            {
                a = ArrayPool<char>.Shared.Rent(maxLenToRedact);
                workBuffer = a;
            }

            inputAsSpan.CopyTo(workBuffer);
            charsInWorkBuffer = inputAsSpan.Length;
        }

        // next step is to add the discriminator

        DiscriminatorSeparator.AsSpan().CopyTo(workBuffer.Slice(charsInWorkBuffer));
        charsInWorkBuffer += DiscriminatorSeparator.Length;

        _discriminator.AsSpan().CopyTo(workBuffer.Slice(charsInWorkBuffer));
        charsInWorkBuffer += _discriminator.Length;

        // final step is to invoke the actual redactor

        var finalToBeRedacted = workBuffer.Slice(0, charsInWorkBuffer);
        result = string.Empty;

        int redactedLen = _redactor!.GetRedactedLength(finalToBeRedacted);
        if (redactedLen > destination.Length)
        {
            if (generateResult)
            {
                var t = ArrayPool<char>.Shared.Rent(redactedLen);
                _redactor!.Redact(finalToBeRedacted, t);
                result = t.AsSpan().Slice(0, redactedLen).ToString();
                ArrayPool<char>.Shared.Return(t);
            }

            charsWritten = 0;

            if (a != null)
            {
                ArrayPool<char>.Shared.Return(a);
            }

            return false;
        }

        charsWritten = _redactor!.Redact(finalToBeRedacted, destination);

        if (a != null)
        {
            ArrayPool<char>.Shared.Return(a);
        }

        return true;
    }

    private static readonly ObjectPool<JustInTimeRedactor> _pool = PoolFactory.CreateResettingPool<JustInTimeRedactor>();
}
