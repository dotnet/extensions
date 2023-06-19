﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Telemetry.Enrichment;
using Microsoft.Extensions.Telemetry.Logging;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Logging.Test;

public static class LogMethodHelperTests
{
    [Fact]
    public static void CollectorContract()
    {
        const string ParamName = "param_name Name";
        const string PropName = "Property Name";
        const string Value = "Value";

        var list = new LogMethodHelper();
        Assert.Equal(list.ParameterName, string.Empty);
        Assert.Empty(list);

        list.ParameterName = ParamName;
        list.Add(PropName, Value);
        Assert.Single(list);
        Assert.Equal(ParamName, list.ParameterName);
        Assert.Equal(ParamName + "_" + PropName, list[0].Key);
        Assert.Equal(Value, list[0].Value);

        _ = list.TryReset();
        Assert.Empty(list);
        Assert.Equal(string.Empty, list.ParameterName);

        list.Add(PropName, Value);
        Assert.Single(list);
        Assert.Equal(PropName, list[0].Key);
        Assert.Equal(Value, list[0].Value);

        var bag = (IEnrichmentPropertyBag)list;

        _ = list.TryReset();
        bag.Add(PropName, Value);
        Assert.Single(list);
        Assert.Equal(PropName, list[0].Key);
        Assert.Equal(Value, list[0].Value);

        _ = list.TryReset();
        bag.Add(PropName, (object)Value);
        Assert.Single(list);
        Assert.Equal(PropName, list[0].Key);
        Assert.Equal(Value, list[0].Value);

        _ = list.TryReset();
        bag.Add(new[] { new KeyValuePair<string, object>(PropName, Value) }.AsSpan());
        Assert.Single(list);
        Assert.Equal(PropName, list[0].Key);
        Assert.Equal(Value, list[0].Value);

        _ = list.TryReset();
        bag.Add(new[] { new KeyValuePair<string, string>(PropName, Value) }.AsSpan());
        Assert.Single(list);
        Assert.Equal(PropName, list[0].Key);
        Assert.Equal(Value, list[0].Value);
    }

    [Theory]
    [InlineData(null, "null")]
    [InlineData(new[] { "One" }, "[\"One\"]")]
    [InlineData(new[] { "One", "Two" }, "[\"One\",\"Two\"]")]
    [InlineData(new[] { "One", null }, "[\"One\",null]")]
    [InlineData(new[] { 1, 2, 3 }, "[\"1\",\"2\",\"3\"]")]
    public static void Enumerate(IEnumerable? enumerable, string expected)
    {
        Assert.Equal(expected, LogMethodHelper.Stringify(enumerable));
    }

    [Fact]
    public static void EnumerateKeyValuePair()
    {
        Assert.Equal("null", LogMethodHelper.Stringify((IEnumerable<KeyValuePair<string, string>>?)null));

        var d0 = new Dictionary<string, string>
        {
            { "One", "Un" }
        };
        Assert.Equal("{\"One\"=\"Un\"}", LogMethodHelper.Stringify(d0));

        var d1 = new Dictionary<string, string>
        {
            { "One", "Un" },
            { "Two", "Deux" }
        };
        Assert.Equal("{\"One\"=\"Un\",\"Two\"=\"Deux\"}", LogMethodHelper.Stringify(d1));

        var d2 = new List<KeyValuePair<string?, string?>>
        {
            new(null, "Un"),
            new("Two", null),
        };
        Assert.Equal("{null=\"Un\",\"Two\"=null}", LogMethodHelper.Stringify(d2));

        var d3 = new Dictionary<string, int>
        {
            { "Zero", 0 },
            { "One", 1 },
            { "Two", 2 }
        };
        Assert.Equal("{\"Zero\"=\"0\",\"One\"=\"1\",\"Two\"=\"2\"}", LogMethodHelper.Stringify(d3));

        var d4 = new Dictionary<int, string>
        {
            { 0, "Zero" },
            { 1, "One" },
            { 2, "Two" }
        };
        Assert.Equal("{\"0\"=\"Zero\",\"1\"=\"One\",\"2\"=\"Two\"}", LogMethodHelper.Stringify(d4));
    }

    [Fact]
    public static void Pool()
    {
        var list = LogMethodHelper.GetHelper();
        Assert.NotNull(list);
        list.Add("Foo", "Bar");
        LogMethodHelper.ReturnHelper(list);
    }

#if NET8_0_OR_GREATER
    // Whilst LogMethodHelper.SkipEnabledCheckOptions is marked as NET6_0_OR_GREATER we don't build .NET 6.0,
    // and as such the API is available in .NET 8 onwards.
    [Fact]
    public static void Options()
    {
        var opt = LogMethodHelper.SkipEnabledCheckOptions;
        Assert.True(opt.SkipEnabledCheck);
    }
#endif
}
