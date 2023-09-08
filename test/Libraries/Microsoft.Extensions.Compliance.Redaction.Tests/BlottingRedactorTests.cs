// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.Compliance.Redaction.Tests;

public class BlottingRedactorTests
{
    [Fact]
    public void Basic()
    {
        const string Hello = "Hello";

        var opt = new BlottingRedactorOptions();
        Assert.Equal('*', opt.BlottingCharacter);

        var redactor = new BlottingRedactor(Microsoft.Extensions.Options.Options.Create(new BlottingRedactorOptions
        {
            BlottingCharacter = 'X'
        }));

        var buffer = new char[Hello.Length];
        var len = redactor.Redact(Hello, buffer);

        Assert.Equal(Hello.Length, len);
        for (int i = 0; i < len; i++)
        {
            Assert.Equal('X', buffer[i]);
        }

        Assert.Equal(Hello.Length, redactor.GetRedactedLength(Hello));
    }

    [Fact]
    public void Does_Nothing_With_Empty_Input()
    {
        var redactor = new BlottingRedactor(Microsoft.Extensions.Options.Options.Create(new BlottingRedactorOptions
        {
            BlottingCharacter = 'X'
        }));

        var written = redactor.Redact(string.Empty, new char[0]);
        Assert.Equal(0, written);
    }
}
