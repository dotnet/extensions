﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.Compliance.Redaction.Test;

public class XxHash3RedactorTests
{
    [Fact]
    public void Basic()
    {
        var redactor = new XxHash3Redactor(Microsoft.Extensions.Options.Options.Create(new XxHash3RedactorOptions
        {
            HashSeed = 101,
        }));

        Assert.Equal(XxHash3Redactor.RedactedSize, redactor.GetRedactedLength(" "));
        Assert.Equal(XxHash3Redactor.RedactedSize, redactor.GetRedactedLength("--"));
        Assert.Equal(XxHash3Redactor.RedactedSize, redactor.GetRedactedLength("XXXXXXXXXXXXXXXXXXXXXXX"));

        var s1 = new char[XxHash3Redactor.RedactedSize];
        var r1 = redactor.Redact("Hello", s1);

        var s2 = new char[XxHash3Redactor.RedactedSize];
        var r2 = redactor.Redact("Hello", s2);

        Assert.Equal(r1, r2);
        Assert.Equal(s1, s2);

        redactor = new XxHash3Redactor(Microsoft.Extensions.Options.Options.Create(new XxHash3RedactorOptions
        {
            HashSeed = 10101,
        }));

        var s3 = new char[XxHash3Redactor.RedactedSize];
        for (int i = 0; i < s3.Length; i++)
        {
            s3[i] = '@';
        }

        var r3 = redactor.Redact("Hello", s3);

        Assert.Equal(r1, r3);
        Assert.NotEqual(s1, s3);
        Assert.DoesNotContain('@', s3);
    }

    [Fact]
    public void XXHashRedactor_Does_Nothing_With_Empty_Input()
    {
        var redactor = new XxHash3Redactor(Microsoft.Extensions.Options.Options.Create(new XxHash3RedactorOptions
        {
            HashSeed = 101,
        }));

        var buffer = new char[5];

        var written = redactor.Redact(string.Empty, buffer);

        Assert.Equal(0, written);

        foreach (var item in buffer)
        {
            Assert.Equal('\0', item);
        }
    }
}