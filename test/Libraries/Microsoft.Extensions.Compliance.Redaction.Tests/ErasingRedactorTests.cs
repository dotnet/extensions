// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.Compliance.Redaction.Test;

public class ErasingRedactorTests
{
    [Fact]
    public void Null_Redactor_Always_Returns_Same_Stuff()
    {
        Redactor redactor = ErasingRedactor.Instance;

        var oneLength = redactor.GetRedactedLength(new char[1]);
        var twoLength = redactor.GetRedactedLength(new char[10]);
        var threeLength = redactor.GetRedactedLength(new char[100]);

        Assert.Equal(oneLength, twoLength);
        Assert.Equal(oneLength, threeLength);
    }

    [Fact]
    public void Null_Redactor_Always_Returns_Same_Stuff_When_Used_As_IRedactor_Of_String()
    {
        Redactor redactor = ErasingRedactor.Instance;

        var oneLength = redactor.GetRedactedLength(new string('a', 1));
        var twoLength = redactor.GetRedactedLength(new string('a', 12));
        var threeLength = redactor.GetRedactedLength(new string('a', 82));

        Assert.Equal(oneLength, twoLength);
        Assert.Equal(oneLength, threeLength);
    }

    [Fact]
    public void Null_Redactor_Doesnt_Mutate_Passed_Buffer()
    {
        var input = new string('G', 20);
        Redactor redactor = ErasingRedactor.Instance;
        Span<char> buffer = stackalloc char[20];

        redactor.Redact(input, buffer);

        var bufferString = buffer.ToString();

        Assert.NotEqual(input, bufferString);
        Assert.DoesNotContain(input, bufferString);
    }

    [Fact]
    public void Null_Redactor_Returns_Empty_String_On_Redact_Span_String_Overload()
    {
        var e = ErasingRedactor.Instance.Redact("any");

        Assert.NotNull(e);
        Assert.Empty(e);
    }
}
