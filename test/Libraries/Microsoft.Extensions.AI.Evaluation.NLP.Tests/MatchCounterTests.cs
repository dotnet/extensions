// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.AI.Evaluation.NLP.Common;
using Xunit;

namespace Microsoft.Extensions.AI.Evaluation.NLP.Tests;

public class MatchCounterTests
{
    [Fact]
    public void EmptyConstructor_InitializesEmptyCounter()
    {
        var counter = new MatchCounter<int>();
        Assert.Empty(counter);
        Assert.Equal(0, counter.Sum());
    }

    [Fact]
    public void ConstructorWithItems_CountsCorrectly()
    {
        var counter = new MatchCounter<string>(new[] { "a", "b", "a", "c", "b", "a" });
        var dict = counter.ToDictionary(kv => kv.Key, kv => kv.Value);
        Assert.Equal(3, dict["a"]);
        Assert.Equal(2, dict["b"]);
        Assert.Equal(1, dict["c"]);
        Assert.Equal(6, counter.Sum());
    }

    [Fact]
    public void Add_AddsSingleItemCorrectly()
    {
        var counter = new MatchCounter<int>();
        counter.Add(5);
        counter.Add(5);
        counter.Add(3);
        var dict = counter.ToDictionary(kv => kv.Key, kv => kv.Value);
        Assert.Equal(2, dict[5]);
        Assert.Equal(1, dict[3]);
        Assert.Equal(3, counter.Sum());
    }

    [Fact]
    public void AddRange_AddsMultipleItemsCorrectly()
    {
        var counter = new MatchCounter<char>();
        counter.AddRange("hello");
        var dict = counter.ToDictionary(kv => kv.Key, kv => kv.Value);
        Assert.Equal(1, dict['h']);
        Assert.Equal(1, dict['e']);
        Assert.Equal(2, dict['l']);
        Assert.Equal(1, dict['o']);
        Assert.Equal(5, counter.Sum());
    }

    [Fact]
    public void ToDebugString_FormatsCorrectly()
    {
        var counter = new MatchCounter<string>(new[] { "x", "y", "x" });
        var str = counter.ToDebugString();
        Assert.Contains("x: 2", str);
        Assert.Contains("y: 1", str);
    }

    [Fact]
    public void Intersect_ReturnsCorrectIntersection()
    {
        MatchCounter<int> counter1 = new(new[] { 1, 2, 2, 3 });
        MatchCounter<int> counter2 = new(new[] { 2, 2, 4 });

        MatchCounter<int> intersection = counter1.Intersect(counter2);
        Dictionary<int, int> dict = intersection.ToDictionary(kv => kv.Key, kv => kv.Value);
        Assert.Equal(2, dict[2]);
        Assert.Equal(2, intersection.Sum());

        intersection = counter2.Intersect(counter1);
        dict = intersection.ToDictionary(kv => kv.Key, kv => kv.Value);
        Assert.Equal(2, dict[2]);
        Assert.Equal(2, intersection.Sum());
    }
}
