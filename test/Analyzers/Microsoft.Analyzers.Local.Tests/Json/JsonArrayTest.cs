// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Forked from StyleCop.Analyzers repo.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.LocalAnalyzers.Json.Test;

public class JsonArrayTest
{
    [Fact]
    public void TestConstructor()
    {
        var obj = new JsonArray();
        Assert.Equal(0, obj.Count);

        var obj1 = new JsonArray(1, "test1");
        Assert.Equal(2, obj1.Count);
        Assert.Equal(1, obj1[0].AsInteger);
        Assert.Equal("test1", obj1[1].AsString);

        var obj2 = new JsonArray { 1, "test2" };
        Assert.Equal(2, obj2.Count);
        Assert.Equal(1, obj2[0].AsInteger);
        Assert.Equal("test2", obj2[1].AsString);

        Assert.Throws<ArgumentNullException>("values", () => new JsonArray(default!));
    }

    [Fact]
    public void TestIndexer()
    {
        var obj = new JsonArray(1);
        Assert.Equal(1, obj.Count);
        Assert.Equal(1, obj[0].AsInteger);
        Assert.Equal(JsonValue.Null, obj[1]);
        Assert.Equal(JsonValue.Null, obj[-1]);

        obj[0] = 2;
        Assert.Equal(2, obj[0].AsInteger);

        Assert.ThrowsAny<ArgumentOutOfRangeException>(() => obj[-1] = 0);
        Assert.ThrowsAny<ArgumentException>(() => obj[1] = 0);
    }

    [Fact]
    public void TestInsert()
    {
        var obj = new JsonArray(1);
        Assert.Equal(1, obj.Count);
        Assert.Equal(1, obj[0].AsInteger);

        // Insert at end
        Assert.Same(obj, obj.Insert(obj.Count, 2));
        Assert.Equal(2, obj.Count);
        Assert.Equal(1, obj[0].AsInteger);
        Assert.Equal(2, obj[1].AsInteger);

        // Insert at beginning
        Assert.Same(obj, obj.Insert(0, 0));
        Assert.Equal(3, obj.Count);
        Assert.Equal(0, obj[0].AsInteger);
        Assert.Equal(1, obj[1].AsInteger);
        Assert.Equal(2, obj[2].AsInteger);

        Assert.ThrowsAny<ArgumentOutOfRangeException>(() => obj.Insert(-1, 0));
        Assert.ThrowsAny<ArgumentException>(() => obj.Insert(obj.Count + 1, 0));
    }

    [Fact]
    public void TestRemove()
    {
        var obj = new JsonArray(0, 1, 2);
        Assert.Equal(3, obj.Count);

        Assert.ThrowsAny<ArgumentOutOfRangeException>(() => obj.Remove(-1));
        Assert.ThrowsAny<ArgumentException>(() => obj.Remove(obj.Count));

        Assert.Same(obj, obj.Remove(1));
        Assert.Equal(2, obj.Count);
        Assert.Equal(0, obj[0].AsInteger);
        Assert.Equal(2, obj[1].AsInteger);
    }

    [Fact]
    public void TestClear()
    {
        var obj = new JsonArray(0, 1, 2);
        Assert.Equal(3, obj.Count);

        Assert.Same(obj, obj.Clear());
        Assert.Equal(0, obj.Count);

        Assert.Same(obj, obj.Clear());
        Assert.Equal(0, obj.Count);
    }

    [Fact]
    public void TestContains()
    {
        var obj = new JsonArray("a", "b", "c");
        Assert.True(obj.Contains("b"));
        obj.Remove(1);
        Assert.False(obj.Contains("b"));

        Assert.False(obj.Contains(JsonValue.Null));
    }

    [Fact]
    public void TestIndexOf()
    {
        var obj = new JsonArray("a", "b", "c");
        Assert.Equal(1, obj.IndexOf("b"));
        Assert.Equal(2, obj.IndexOf("c"));
        obj.Remove(1);
        Assert.Equal(-1, obj.IndexOf("b"));
        Assert.Equal(1, obj.IndexOf("c"));

        Assert.Equal(-1, obj.IndexOf(JsonValue.Null));
    }

    [Fact]
    public void TestEnumerators()
    {
        var obj = new JsonArray("a", "b", "c");

        using var genericEnumerator = obj.GetEnumerator();
        var legacyEnumerator = ((IEnumerable)obj).GetEnumerator();

        for (int i = 0; i < obj.Count; i++)
        {
            Assert.True(genericEnumerator.MoveNext());
            Assert.True(legacyEnumerator.MoveNext());
            Assert.Equal(obj[i], genericEnumerator.Current);
            Assert.Equal(obj[i], legacyEnumerator.Current);
            Assert.Equal(genericEnumerator.Current, legacyEnumerator.Current);
        }
    }
}
