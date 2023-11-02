// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using Microsoft.Extensions.Compliance.Testing;
using Xunit;

namespace Microsoft.Extensions.Logging.Test;

public static class JustInTimeRedactorTests
{
#pragma warning disable S103
    [Theory]
    [InlineData("ABC", "", "ABC")]
    [InlineData("ABC", "123", "ABC:123")]
    [InlineData(789, "", "789")]
    [InlineData(789, "123", "789:123")]
    [InlineData(new[] { 'A', 'B', 'C' }, "", "ABC")]
    [InlineData(new[] { 'A', 'B', 'C' }, "123", "ABC:123")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZ", "", "ABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZ")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZ", "123", "ABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZ:123")]
    [InlineData("1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567812345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678", "", "1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567812345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678")]
    [InlineData("1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567812345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678", "123", "1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567812345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678:123")]
    public static void Basic(object value, string discriminator, string redactorInput)
    {
        const string ShortRedactorPrefix = "REDACTED";
        string longRedactorPrefix = new('X', 256);

        var shortRedactor = FakeRedactor.Create(new FakeRedactorOptions
        {
            RedactionFormat = $"{ShortRedactorPrefix}<{{0}}>",
        });

        var longRedactor = FakeRedactor.Create(new FakeRedactorOptions
        {
            RedactionFormat = $"{longRedactorPrefix}<{{0}}>",
        });

        // -- short redactor

        var r = JustInTimeRedactor.Get(value, shortRedactor, discriminator);
        Assert.Equal($"{ShortRedactorPrefix}<{redactorInput}>", r.ToString());

        Span<char> d = new char[1024];
        Assert.True(r.TryFormat(d, out int charsWritten, string.Empty.AsSpan(), CultureInfo.InvariantCulture));
        Assert.Equal($"{ShortRedactorPrefix}<{redactorInput}>", d.Slice(0, charsWritten).ToString());

        d = new char[2];
        Assert.False(r.TryFormat(d, out int _, string.Empty.AsSpan(), CultureInfo.InvariantCulture));

        r.Return();

        r = JustInTimeRedactor.Get(new Formattable(value), shortRedactor, discriminator);
        Assert.Equal($"{ShortRedactorPrefix}<{redactorInput}>", r.ToString());
        r.Return();

#if NET6_0_OR_GREATER
        r = JustInTimeRedactor.Get(new SpanFormattable(value), shortRedactor, discriminator);
        Assert.Equal($"{ShortRedactorPrefix}<{redactorInput}>", r.ToString());
        r.Return();
#endif

        // -- long redactor

        r = JustInTimeRedactor.Get(value, longRedactor, discriminator);
        Assert.Equal($"{longRedactorPrefix}<{redactorInput}>", r.ToString());

        d = new char[1024];
        Assert.True(r.TryFormat(d, out charsWritten, string.Empty.AsSpan(), CultureInfo.InvariantCulture));
        Assert.Equal($"{longRedactorPrefix}<{redactorInput}>", d.Slice(0, charsWritten).ToString());

        r.Return();

        r = JustInTimeRedactor.Get(new Formattable(value), longRedactor, discriminator);
        Assert.Equal($"{longRedactorPrefix}<{redactorInput}>", r.ToString());
        r.Return();

#if NET6_0_OR_GREATER
        r = JustInTimeRedactor.Get(new SpanFormattable(value), longRedactor, discriminator);
        Assert.Equal($"{longRedactorPrefix}<{redactorInput}>", r.ToString());
        r.Return();
#endif
    }
#pragma warning restore S103

    private sealed class Formattable : IFormattable
    {
        private readonly object? _value;

        public Formattable(object? value)
        {
            if (value is char[] c)
            {
                value = new string(c);
            }

            _value = value;
        }

        public string ToString(string? format, IFormatProvider? provider) => Convert.ToString(_value, provider)!;
    }

#if NET6_0_OR_GREATER
    private sealed class SpanFormattable : ISpanFormattable
    {
        private readonly object? _value;

        public SpanFormattable(object? value)
        {
            if (value is char[] c)
            {
                value = new string(c);
            }

            _value = value;
        }

        public string ToString(string? format, IFormatProvider? provider) => Convert.ToString(_value, provider)!;

        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            var s = Convert.ToString(_value, provider)!;
            if (s.Length > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            s.CopyTo(destination);
            charsWritten = s.Length;

            return true;
        }
    }
#endif
}
