// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Compliance.Classification;
using Xunit;

namespace Microsoft.Extensions.Compliance.Redaction.Tests;

public static class NullRedactorTest
{
    [Fact]
    public static void NullRedactor_When_Given_Empty_String_Returns_Empty_String()
    {
        var r = NullRedactor.Instance;

        var emptyStringRedacted = r.Redact(string.Empty);

        Assert.Equal(string.Empty, emptyStringRedacted);
    }

    [Fact]
    public static void NullRedactor_When_Given_Empty_Buffer_Returns_0_Chars_Written()
    {
        var r = NullRedactor.Instance;

        Span<char> input = stackalloc char[0];

        var c = new char[1];

        var charsWritten = r.Redact(input, c);

        Assert.Equal(0, charsWritten);
        Assert.Equal('\0', c[0]);
    }

    [Fact]
    public static void NullRedactor_Handles_BufferTooSmall()
    {
        var r = NullRedactor.Instance;
        Assert.Throws<ArgumentException>(() => r.Redact("ABCD".AsSpan(), new char[1].AsSpan()));
    }

    [Fact]
    public static void NullRedactorProvider_Returns_Always_NullRedactor()
    {
        var dc1 = new DataClassification("TAX", 1);
        var dc2 = new DataClassification("TAX", 2);
        var dc3 = new DataClassification("TAX", 4);

        var rp = NullRedactorProvider.Instance;
        var redactor1 = NullRedactor.Instance;
        var redactor2 = rp.GetRedactor(dc1);
        var redactor3 = rp.GetRedactor(dc2);
        var redactor4 = rp.GetRedactor(dc3);

        Assert.Equal(redactor1, redactor2);
        Assert.Equal(redactor1, redactor3);
        Assert.Equal(redactor1, redactor4);
        Assert.IsAssignableFrom<NullRedactor>(redactor1);
    }
}
