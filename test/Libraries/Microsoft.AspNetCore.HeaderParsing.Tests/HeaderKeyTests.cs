// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.HeaderParsing.Parsers;
using Xunit;

namespace Microsoft.AspNetCore.HeaderParsing.Test;

public class HeaderKeyTests
{
    private const string TestHeaderName = "Test-Header";
    private const int TestHeaderPosition = 5;
    private static readonly DateTimeOffset _testHeaderDefaultValue = DateTimeOffset.Now;

    [Fact]
    public void ToString_returns_header_name()
    {
        var sut = new HeaderKey<DateTimeOffset>(TestHeaderName, DateTimeOffsetParser.Instance, TestHeaderPosition);
        Assert.Equal(TestHeaderName, sut.ToString());
    }

    [Fact]
    public void Ctor_propagates_arguments_to_properties()
    {
        var sut = new HeaderKey<DateTimeOffset>(TestHeaderName, DateTimeOffsetParser.Instance, TestHeaderPosition);

        Assert.Equal(TestHeaderName, sut.Name);
        Assert.Equal(DateTimeOffsetParser.Instance, sut.Parser);
        Assert.Equal(TestHeaderPosition, sut.Position);
    }

    [Fact]
    public void DefaultValue_returns_default_when_no_value_set()
    {
        var referenceTimeDefault = new HeaderKey<IReadOnlyList<IPAddress>>(TestHeaderName, IPAddressListParser.Instance, TestHeaderPosition);
        var valueTypeDefault = new HeaderKey<DateTimeOffset>(TestHeaderName, DateTimeOffsetParser.Instance, TestHeaderPosition);

        Assert.False(referenceTimeDefault.HasDefaultValue);
        Assert.Null(referenceTimeDefault.DefaultValue);

        Assert.False(valueTypeDefault.HasDefaultValue);
        Assert.Equal(default, valueTypeDefault.DefaultValue);
    }

    [Fact]
    public void DefaultValue_returns_default_value()
    {
        var sut = new HeaderKey<DateTimeOffset>(TestHeaderName, DateTimeOffsetParser.Instance, TestHeaderPosition, 0, _testHeaderDefaultValue);

        Assert.True(sut.HasDefaultValue);
        Assert.Equal(_testHeaderDefaultValue, sut.DefaultValue);
    }
}
