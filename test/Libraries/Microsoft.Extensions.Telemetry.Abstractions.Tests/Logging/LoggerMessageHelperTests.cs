// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.Logging.Test;

public static class LoggerMessageHelperTests
{
    [Theory]
    [InlineData(null, "null")]
    [InlineData(new[] { "One" }, "[\"One\"]")]
    [InlineData(new[] { "One", "Two" }, "[\"One\",\"Two\"]")]
    [InlineData(new[] { "One", null }, "[\"One\",null]")]
    [InlineData(new[] { 1, 2, 3 }, "[\"1\",\"2\",\"3\"]")]
    public static void Enumerate(IEnumerable? enumerable, string expected)
    {
        Assert.Equal(expected, LoggerMessageHelper.Stringify(enumerable));
    }

    [Fact]
    public static void EnumerateKeyValuePair()
    {
        Assert.Equal("null", LoggerMessageHelper.Stringify((IEnumerable<KeyValuePair<string, string>>?)null));

        var d0 = new Dictionary<string, string>
        {
            { "One", "Un" }
        };
        Assert.Equal("{\"One\"=\"Un\"}", LoggerMessageHelper.Stringify(d0));

        var d1 = new Dictionary<string, string>
        {
            { "One", "Un" },
            { "Two", "Deux" }
        };
        Assert.Equal("{\"One\"=\"Un\",\"Two\"=\"Deux\"}", LoggerMessageHelper.Stringify(d1));

        var d2 = new List<KeyValuePair<string?, string?>>
        {
            new(null, "Un"),
            new("Two", null),
        };
        Assert.Equal("{null=\"Un\",\"Two\"=null}", LoggerMessageHelper.Stringify(d2));

        var d3 = new Dictionary<string, int>
        {
            { "Zero", 0 },
            { "One", 1 },
            { "Two", 2 }
        };
        Assert.Equal("{\"Zero\"=\"0\",\"One\"=\"1\",\"Two\"=\"2\"}", LoggerMessageHelper.Stringify(d3));

        var d4 = new Dictionary<int, string>
        {
            { 0, "Zero" },
            { 1, "One" },
            { 2, "Two" }
        };
        Assert.Equal("{\"0\"=\"Zero\",\"1\"=\"One\",\"2\"=\"Two\"}", LoggerMessageHelper.Stringify(d4));
    }

    [Fact]
    public static void ThreadLocal()
    {
        var lmp1 = LoggerMessageHelper.ThreadLocalState;
        Assert.NotNull(lmp1);

        var lmp2 = LoggerMessageHelper.ThreadLocalState;
        Assert.Same(lmp1, lmp2);
    }

#if NET6_0_OR_GREATER
    [Fact]
    public static void Options()
    {
        var opt = LogMethodHelper.SkipEnabledCheckOptions;
        Assert.True(opt.SkipEnabledCheck);
    }
#endif
}
