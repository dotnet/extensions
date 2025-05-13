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
        var args = new AIFunctionArguments();
        Assert.Null(args.Services);
        Assert.Empty(args);

        args.Add("key", "value");
        Assert.Single(args);
    }

    [Fact]
    public void EmptyArg_RoundtripsAsEmpty()
    {
        var args = new AIFunctionArguments(new Dictionary<string, object?>());
        Assert.Null(args.Services);
        Assert.Empty(args);

        args.Add("key", "value");
        Assert.Single(args);
    }

    [Fact]
    public void NonEmptyArg_RoundtripsAsEmpty()
    {
        var args = new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["key"] = "value"
        });
        Assert.Null(args.Services);
        Assert.Single(args);
    }

    [Fact]
    public void Services_Roundtrips()
    {
        ServiceCollection sc = new();
        IServiceProvider sp = sc.BuildServiceProvider();

        var args = new AIFunctionArguments
        {
            Services = sp
        };

        Assert.Same(sp, args.Services);
        Assert.Empty(args);

        args.Add("key", "value");
        Assert.Single(args);
    }

    [Fact]
    public void IReadOnlyDictionary_ImplementsInterface()
    {
        ServiceCollection sc = new();
        IServiceProvider sp = sc.BuildServiceProvider();

        IReadOnlyDictionary<string, object?> args = new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["key1"] = "value1",
            ["key2"] = "value2",
        });

        Assert.Equal(2, args.Count);

        Assert.True(args.ContainsKey("key1"));
        Assert.True(args.ContainsKey("key2"));
        Assert.False(args.ContainsKey("KEY1"));

        Assert.Equal(["key1", "key2"], args.Keys);
        Assert.Equal(["value1", "value2"], args.Values);

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

    [Fact]
    public void IDictionary_ImplementsInterface()
    {
        ServiceCollection sc = new();
        IServiceProvider sp = sc.BuildServiceProvider();

        IDictionary<string, object?> args = new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["key1"] = "value1",
            ["key2"] = "value2",
        });

        Assert.Equal(2, args.Count);
        Assert.False(args.IsReadOnly);

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

        args.Add("key3", "value3");
        Assert.Equal(3, args.Count);
        Assert.True(args.ContainsKey("key3"));
        Assert.Equal("value3", args["key3"]);

        args["key4"] = "value4";
        Assert.Equal(4, args.Count);
        Assert.True(args.ContainsKey("key4"));
        Assert.Equal("value4", args["key4"]);

        args.Remove("key1");
        Assert.Equal(3, args.Count);
        Assert.False(args.ContainsKey("key1"));
        Assert.Equal("value2", args["key2"]);
        Assert.Equal("value3", args["key3"]);
        Assert.Equal("value4", args["key4"]);

        args.Clear();
        Assert.Empty(args);

        args.Add(new KeyValuePair<string, object?>("key1", "value1"));
        Assert.Single(args);
        Assert.True(args.ContainsKey("key1"));
        Assert.Equal("value1", args["key1"]);

        args.Add(new KeyValuePair<string, object?>("key2", "value2"));
        Assert.Equal(2, args.Count);
        Assert.True(args.ContainsKey("key2"));
        Assert.Equal("value2", args["key2"]);

        Assert.Equal(["key1", "key2"], args.Keys);
        Assert.Equal(["value1", "value2"], args.Values);
    }

    [Fact]
    public void Constructor_WithComparer_CreatesEmptyDictionary()
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var args = new AIFunctionArguments(comparer);

        Assert.Null(args.Services);
        Assert.Empty(args);

        // Verify case insensitivity
        args.Add("key", "value");
        Assert.Single(args);
        Assert.True(args.ContainsKey("KEY"));
        Assert.Equal("value", args["KEY"]);
    }

    [Fact]
    public void Constructor_WithComparerAndNullArguments_CreatesEmptyDictionary()
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var args = new AIFunctionArguments(null, comparer);

        Assert.Null(args.Services);
        Assert.Empty(args);

        // Verify case insensitivity
        args.Add("key", "value");
        Assert.Single(args);
        Assert.True(args.ContainsKey("KEY"));
        Assert.Equal("value", args["KEY"]);
    }

    [Fact]
    public void Constructor_WithComparerAndArguments_CopiesArguments()
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var originalArgs = new Dictionary<string, object?>
        {
            ["key1"] = "value1",
            ["key2"] = "value2"
        };

        var args = new AIFunctionArguments(originalArgs, comparer);

        Assert.Null(args.Services);
        Assert.Equal(2, args.Count);
        Assert.True(args.ContainsKey("KEY1"));
        Assert.True(args.ContainsKey("KEY2"));
        Assert.Equal("value1", args["KEY1"]);
        Assert.Equal("value2", args["KEY2"]);

        // Verify that adding to original doesn't affect our copy
        originalArgs["key3"] = "value3";
        Assert.Equal(2, args.Count);
        Assert.False(args.ContainsKey("key3"));

        // Verify case insensitivity for new additions
        args.Add("key4", "value4");
        Assert.Equal(3, args.Count);
        Assert.True(args.ContainsKey("KEY4"));
        Assert.Equal("value4", args["KEY4"]);
    }

    [Fact]
    public void Constructor_ReusesDictionaryInstance_WhenSameComparer()
    {
        // Arrange
        var comparer = StringComparer.OrdinalIgnoreCase;
        var originalDict = new Dictionary<string, object?>(comparer)
        {
            ["key1"] = "value1",
            ["key2"] = "value2"
        };

        // Act
        var args = new AIFunctionArguments(originalDict, comparer);

        // Assert

        // Verify modifications affect both instances
        originalDict["key3"] = "value3";
        Assert.Equal("value3", args["key3"]);

        args["key4"] = "value4";
        Assert.Equal("value4", originalDict["key4"]);
    }

    [Fact]
    public void Constructor_ReusesDictionaryInstance_WhenIsDictionaryWithoutComparerProvided()
    {
        // Arrange
        var comparer = StringComparer.OrdinalIgnoreCase;
        var originalDict = new Dictionary<string, object?>(comparer)
        {
            ["key1"] = "value1",
            ["key2"] = "value2"
        };

        // Act
        var args = new AIFunctionArguments(originalDict);
        var args2 = new AIFunctionArguments(originalDict, null);

        // Assert

        // Verify modifications affect both instances
        originalDict["key3"] = "value3";
        Assert.Equal("value3", args["key3"]);
        Assert.Equal("value3", args2["key3"]);

        args["key4"] = "value4";
        args2["newKey"] = "new value";
        Assert.Equal("value4", originalDict["key4"]);
        Assert.Equal("new value", originalDict["newKey"]);
    }

    [Fact]
    public void Constructor_CreateNewDictionary_WhenDifferentComparer()
    {
        // Arrange
        var originalDict = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["key1"] = "value1",
            ["key2"] = "value2"
        };

        // Act
        var args = new AIFunctionArguments(originalDict, StringComparer.OrdinalIgnoreCase);

        // Assert

        // Verify modifications don't affect each other
        originalDict["key3"] = "value3";
        Assert.False(args.ContainsKey("key3"));

        args["key4"] = "value4";
        Assert.False(originalDict.ContainsKey("key4"));
    }
}
