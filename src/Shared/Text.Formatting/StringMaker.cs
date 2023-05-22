// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

#pragma warning disable CA1716
namespace Microsoft.Shared.Text;
#pragma warning restore CA1716

#pragma warning disable IDE0064

#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
internal ref struct StringMaker
{
    public const int DefaultCapacity = 128;

    private readonly bool _fixedCapacity;
    private char[]? _rentedBuffer;
    private Span<char> _chars;

    public StringMaker(Span<char> initialBuffer, bool fixedCapacity = false)
    {
        _rentedBuffer = null;
        _chars = initialBuffer;
        _fixedCapacity = fixedCapacity;
        Length = 0;
        Overflowed = false;
    }

    public StringMaker(int initialCapacity)
    {
        _rentedBuffer = ArrayPool<char>.Shared.Rent(initialCapacity);
        _chars = _rentedBuffer;
        _fixedCapacity = false;
        Length = 0;
        Overflowed = false;
    }

    public void Dispose()
    {
        if (_rentedBuffer != null)
        {
            ArrayPool<char>.Shared.Return(_rentedBuffer);
        }

        // clear out everything to prevent accidental reuse
        this = default;
    }

    public string ExtractString() => _chars.Slice(0, Length).ToString();
    public ReadOnlySpan<char> ExtractSpan() => _chars.Slice(0, Length);

#if NETCOREAPP3_1_OR_GREATER
    internal void AppendTo(StringBuilder sb) => _ = sb.Append(_chars.Slice(0, Length));
#else
    internal void AppendTo(StringBuilder sb) => _ = sb.Append(_chars.Slice(0, Length).ToString());
#endif
    public int Length { get; private set; }
    public bool Overflowed { get; private set; }

    public void Fill(char value, int count)
    {
        if (!Ensure(count))
        {
            return;
        }

        _chars.Slice(Length, count).Fill(value);
        Length += count;
    }

    public void Append(string? value, int width)
    {
        if (value == null)
        {
            Fill(' ', width);
        }
        else if (width == 0)
        {
            if (!Ensure(value.Length))
            {
                return;
            }

            value.AsSpan().CopyTo(_chars.Slice(Length));
            Length += value.Length;
        }
        else if (width > value.Length)
        {
            Fill(' ', width - value.Length);
            FinishAppend(value, 0);
        }
        else
        {
            FinishAppend(value, width);
        }
    }

    public void Append(ReadOnlySpan<char> value)
    {
        if (!Ensure(value.Length))
        {
            return;
        }

        value.CopyTo(_chars.Slice(Length));
        Length += value.Length;
    }

    public void Append(ReadOnlySpan<char> value, int width)
    {
        if (width == 0)
        {
            if (!Ensure(value.Length))
            {
                return;
            }

            value.CopyTo(_chars.Slice(Length));
            Length += value.Length;
        }
        else if (width > value.Length)
        {
            Fill(' ', width - value.Length);
            FinishAppend(value, 0);
        }
        else
        {
            FinishAppend(value, width);
        }
    }

    public void Append(char value)
    {
        if (!Ensure(1))
        {
            return;
        }

        _chars[Length++] = value;
    }

    public void Append(char value, int width)
    {
        if (width >= -1 && width <= 1)
        {
            if (!Ensure(1))
            {
                return;
            }

            _chars[Length++] = value;
        }
        else if (width > 1)
        {
            if (!Ensure(width))
            {
                return;
            }

            _chars.Slice(Length, width - 1).Fill(' ');
            Length += width;
            _chars[Length - 1] = value;
        }
        else
        {
            width = -width;
            if (!Ensure(width))
            {
                return;
            }

            _chars[Length++] = value;
            _chars.Slice(Length, width - 1).Fill(' ');
            Length += width - 1;
        }
    }

#if !NET6_0_OR_GREATER
    public void Append(long value, string? format, IFormatProvider? provider, int width)
    {
        int charsWritten;
        while (!value.TryFormat(_chars.Slice(Length), out charsWritten, format, provider))
        {
            if (!Expand())
            {
                return;
            }
        }

        FinishAppend(charsWritten, width);
    }

    public void Append(ulong value, string? format, IFormatProvider? provider, int width)
    {
        int charsWritten;
        while (!value.TryFormat(_chars.Slice(Length), out charsWritten, format, provider))
        {
            if (!Expand())
            {
                return;
            }
        }

        FinishAppend(charsWritten, width);
    }

    public void Append(double value, string? format, IFormatProvider? provider, int width)
    {
        int charsWritten;
        while (!value.TryFormat(_chars.Slice(Length), out charsWritten, format, provider))
        {
            if (!Expand())
            {
                return;
            }
        }

        FinishAppend(charsWritten, width);
    }

    public void Append(bool value, int width)
    {
        int charsWritten;
        while (!value.TryFormat(_chars.Slice(Length), out charsWritten))
        {
            if (!Expand())
            {
                return;
            }
        }

        FinishAppend(charsWritten, width);
    }

    public void Append(decimal value, string? format, IFormatProvider? provider, int width)
    {
        int charsWritten;
        while (!value.TryFormat(_chars.Slice(Length), out charsWritten, format, provider))
        {
            if (!Expand())
            {
                return;
            }
        }

        FinishAppend(charsWritten, width);
    }

    public void Append(DateTime value, string? format, IFormatProvider? provider, int width)
    {
        int charsWritten;
        while (!value.TryFormat(_chars.Slice(Length), out charsWritten, format, provider))
        {
            if (!Expand())
            {
                return;
            }
        }

        FinishAppend(charsWritten, width);
    }

    public void Append(TimeSpan value, string? format, IFormatProvider? provider, int width)
    {
        int charsWritten;
        while (!value.TryFormat(_chars.Slice(Length), out charsWritten, format, provider))
        {
            if (!Expand())
            {
                return;
            }
        }

        FinishAppend(charsWritten, width);
    }
#endif

#if NET6_0_OR_GREATER
    public void Append<T>(T value, string? format, IFormatProvider? provider, int width)
        where T : System.ISpanFormattable
    {
        int charsWritten;
        while (!value.TryFormat(_chars.Slice(Length), out charsWritten, format.AsSpan(), provider))
        {
            if (!Expand())
            {
                return;
            }
        }

        FinishAppend(charsWritten, width);
    }
#endif

    public void Append(IFormattable value, string? format, IFormatProvider? provider, int width)
    {
        FinishAppend(value.ToString(format, provider), width);
    }

    public void Append(object? value, int width)
    {
        if (value == null)
        {
            FinishAppend(string.Empty, width);
            return;
        }

        FinishAppend(value.ToString(), width);
    }

    private void FinishAppend(int charsWritten, int width)
    {
        Length += charsWritten;

        var leftAlign = false;
        if (width < 0)
        {
            width = -width;
            leftAlign = true;
        }

        int padding = width - charsWritten;
        if (padding > 0)
        {
            if (!Ensure(padding))
            {
                return;
            }

            if (leftAlign)
            {
                _chars.Slice(Length, padding).Fill(' ');
            }
            else
            {
                int start = Length - charsWritten;
                _chars.Slice(start, charsWritten).CopyTo(_chars.Slice(start + padding));
                _chars.Slice(start, padding).Fill(' ');
            }

            Length += padding;
        }
    }

#if !NETCOREAPP3_1_OR_GREATER
    private void FinishAppend(string result, int width) => FinishAppend(result.AsSpan(), width);
#endif

    private void FinishAppend(ReadOnlySpan<char> result, int width)
    {
        var leftAlign = false;
        if (width < 0)
        {
            width = -width;
            leftAlign = true;
        }

        int padding = width - result.Length;
        int extra = result.Length;
        if (padding > 0)
        {
            extra += padding;
        }

        if (!Ensure(extra))
        {
            return;
        }

        if (padding > 0)
        {
            if (leftAlign)
            {
                result.CopyTo(_chars.Slice(Length));
                _chars.Slice(Length + result.Length, padding).Fill(' ');
            }
            else
            {
                _chars.Slice(Length, padding).Fill(' ');
                result.CopyTo(_chars.Slice(Length + padding));
            }
        }
        else
        {
            result.CopyTo(_chars.Slice(Length));
        }

        Length += extra;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Ensure(int neededCapacity)
    {
        if (Length <= _chars.Length - neededCapacity)
        {
            return true;
        }

        return Expand(neededCapacity);
    }

    private bool Expand(int neededCapacity = 0)
    {
        if (_fixedCapacity)
        {
            Overflowed = true;
            return false;
        }

        if (neededCapacity == 0)
        {
            neededCapacity = DefaultCapacity;
        }

        int newCapacity = _chars.Length + neededCapacity;

        // allocate a new array and copy the existing data to it
        var a = ArrayPool<char>.Shared.Rent(newCapacity);
        _chars.Slice(0, Length).CopyTo(a);

        if (_rentedBuffer != null)
        {
            ArrayPool<char>.Shared.Return(_rentedBuffer);
        }

        _rentedBuffer = a;
        _chars = a;
        return true;
    }
}
