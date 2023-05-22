// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.Compliance.Redaction.Tests;

public class XXHash3RedactorTests
{
    [Fact]
    public void Basic()
    {
        var redactor = new XXHash3Redactor(Microsoft.Extensions.Options.Options.Create(new XXHash3RedactorOptions
        {
            HashSeed = 101,
        }));

        Assert.Equal(XXHash3Redactor.RedactedSize, redactor.GetRedactedLength(" "));
        Assert.Equal(XXHash3Redactor.RedactedSize, redactor.GetRedactedLength("--"));
        Assert.Equal(XXHash3Redactor.RedactedSize, redactor.GetRedactedLength("XXXXXXXXXXXXXXXXXXXXXXX"));

        var s1 = new char[XXHash3Redactor.RedactedSize];
        var r1 = redactor.Redact("Hello", s1);

        var s2 = new char[XXHash3Redactor.RedactedSize];
        var r2 = redactor.Redact("Hello", s2);

        Assert.Equal(r1, r2);
        Assert.Equal(s1, s2);

        redactor = new XXHash3Redactor(Microsoft.Extensions.Options.Options.Create(new XXHash3RedactorOptions
        {
            HashSeed = 10101,
        }));

        var s3 = new char[XXHash3Redactor.RedactedSize];
        var r3 = redactor.Redact("Hello", s3);

        Assert.Equal(r1, r3);
        Assert.NotEqual(s1, s3);
    }

    [Fact]
    public void NullChecks()
    {
        Assert.Throws<ArgumentException>(() => new XXHash3Redactor(Microsoft.Extensions.Options.Options.Create<XXHash3RedactorOptions>(null!)));
    }

    [Fact]
    public void XXHashRedactor_Does_Nothing_With_Empty_Input()
    {
        var redactor = new XXHash3Redactor(Microsoft.Extensions.Options.Options.Create(new XXHash3RedactorOptions
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
