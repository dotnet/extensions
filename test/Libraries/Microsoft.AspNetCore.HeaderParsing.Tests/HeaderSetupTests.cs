// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.HeaderParsing.Parsers;
using Xunit;

namespace Microsoft.AspNetCore.HeaderParsing.Test;

public class HeaderSetupTests
{
    private const string TestHeaderName = "Test-Header";

    [Fact]
    public void New_with_parser_instance()
    {
        var sut = new HeaderSetup<DateTimeOffset>(TestHeaderName, DateTimeOffsetParser.Instance);

        Assert.Equal(TestHeaderName, sut.HeaderName);
        Assert.Equal(DateTimeOffsetParser.Instance, sut.ParserInstance);
        Assert.Null(sut.ParserType);
    }

    [Fact]
    public void New_with_parser_type()
    {
        var sut = new HeaderSetup<DateTimeOffset>(TestHeaderName, typeof(DateTimeOffsetParser));

        Assert.Equal(TestHeaderName, sut.HeaderName);
        Assert.Equal(typeof(DateTimeOffsetParser), sut.ParserType);
        Assert.Null(sut.ParserInstance);
    }
}
