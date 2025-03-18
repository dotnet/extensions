// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.AI;

public class AIFunctionArgumentsTests
{
    [Fact]
    public void NullArg_RoundtripsAsEmpty()
    {
        var args = new AIFunctionArguments(null);
        Assert.Null(args.Services);
    }

    [Fact]
    public void EmptyArg_RoundtripsAsEmpty()
    {
        var args = new AIFunctionArguments([]);
        Assert.Null(args.Services);
    }

    [Fact]
    public void Services_Roundtrips()
    {
        ServiceCollection sc = new();
        IServiceProvider sp = sc.BuildServiceProvider();

        var args = new AIFunctionArguments([])
        {
            Services = sp
        };

        Assert.Same(sp, args.Services);
    }

    [Fact]
    public void IReadOnlyDictionary_ImplementsInterface()
    {
        ServiceCollection sc = new();
        IServiceProvider sp = sc.BuildServiceProvider();

        var args = new AIFunctionArguments(
        [
            new KeyValuePair<string, object?>("key1", "value1"),
            new KeyValuePair<string, object?>("key2", "value2"),
        ])
        {
            Services = sp
        };

        Assert.Same(sp, args.Services);

        Assert.Equal(2, args.Count);

        Assert.True(args.ContainsKey("key1"));
        Assert.True(args.ContainsKey("key2"));
        Assert.False(args.ContainsKey("KEY1"));

        Assert.Equal("value1", args["key1"]);
        Assert.Equal("value2", args["key2"]);

        Assert.Equal(new[] { "key1", "key2" }, args.Keys);
        Assert.Equal(new[] { "value1", "value2" }, args.Values);

        Assert.True(args.TryGetValue("key1", out var value));
        Assert.Equal("value1", value);
        Assert.False(args.TryGetValue("key3", out value));
        Assert.Null(value);

        Assert.Equal([
            new KeyValuePair<string, object?>("key1", "value1"),
            new KeyValuePair<string, object?>("key2", "value2"),
        ], args.ToArray());
    }
}
