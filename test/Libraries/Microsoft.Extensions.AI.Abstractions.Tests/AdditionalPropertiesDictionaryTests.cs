// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.AI;

public class AdditionalPropertiesDictionaryTests
{
    [Fact]
    public void Constructor_Roundtrips()
    {
        AdditionalPropertiesDictionary d = new();
        Assert.Empty(d);

        d = new(new Dictionary<string, object?> { ["key1"] = "value1" });
        Assert.Single(d);

        d = new((IEnumerable<KeyValuePair<string, object?>>)new Dictionary<string, object?> { ["key1"] = "value1", ["key2"] = "value2" });
        Assert.Equal(2, d.Count);
    }

    [Fact]
    public void Comparer_OrdinalIgnoreCase()
    {
        AdditionalPropertiesDictionary d = new()
        {
            ["key1"] = "value1",
            ["KEY1"] = "value2",
            ["key2"] = "value3",
            ["key3"] = "value4",
            ["KeY3"] = "value5",
        };

        Assert.Equal(3, d.Count);

        Assert.Equal("value2", d["key1"]);
        Assert.Equal("value2", d["kEY1"]);

        Assert.Equal("value3", d["key2"]);
        Assert.Equal("value3", d["KEY2"]);

        Assert.Equal("value5", d["Key3"]);
        Assert.Equal("value5", d["KEy3"]);
    }

    [Fact]
    public void TryGetValue_Typed_ExtractsExpectedValue()
    {
        AssertFound(42, 42L);
        AssertFound(42, 42.0);
        AssertFound(42, 42f);
        AssertFound(42, true);
        AssertFound(42, "42");
        AssertFound(42, (object)42);
        AssertFound(42.0, 42f);
        AssertFound(42f, 42.0);
        AssertFound(42m, 42.0f);
        AssertFound(42L, 42);
        AssertFound("42", "42");
        AssertFound("42", 42);
        AssertFound("42", 42L);
        AssertFound("42", 42.0);
        AssertFound("42", 42f);
        AssertFound(true, 1);
        AssertFound(false, 0);

        AssertNotFound<int, DateTime>(42);
        AssertNotFound<int, AdditionalPropertiesDictionaryTests>(42);

        static void AssertFound<T1, T2>(T1 input, T2 expected)
        {
            AdditionalPropertiesDictionary d = [];
            d["key"] = input;

            Assert.True(d.TryGetValue("key", out T2? value));
            Assert.Equal(expected, value);

            Assert.False(d.TryGetValue("key2", out value));
            Assert.Equal(default, value);
        }

        static void AssertNotFound<T1, T2>(T1 input)
        {
            AdditionalPropertiesDictionary d = [];
            d["key"] = input;
            Assert.False(d.TryGetValue("key", out T2? value));
            Assert.Equal(default(T2), value);
        }
    }
}
