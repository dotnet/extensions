// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Forked from StyleCop.Analyzers repo.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.LocalAnalyzers.Json.Test;

public class JsonObjectTest
{
    [Fact]
    public void TestCount()
    {
        var obj = new JsonObject();
        Assert.Equal(0, obj.Count);

        obj["x"] = "value";
        Assert.Equal(1, obj.Count);

        obj["y"] = "value";
        Assert.Equal(2, obj.Count);

        obj["x"] = "value2";
        Assert.Equal(2, obj.Count);

        Assert.True(obj.Remove("x"));
        Assert.Equal(1, obj.Count);

        Assert.False(obj.Remove("x"));
        Assert.Equal(1, obj.Count);

        obj["z"] = "value3";
        Assert.Equal(2, obj.Count);

        Assert.Same(obj, obj.Clear());
        Assert.Equal(0, obj.Count);
    }

    [Fact]
    public void TestAdd()
    {
        var obj = new JsonObject();
        Assert.Equal(JsonValue.Null, obj["x"]);
        Assert.False(obj.ContainsKey("x"));

        Assert.Same(obj, obj.Add("x"));
        Assert.Equal(JsonValue.Null, obj["x"]);
        Assert.True(obj.ContainsKey("x"));
    }

    [Fact]
    public void TestEnumerator()
    {
        var obj = new JsonObject
        {
            ["x"] = "x1",
            ["y"] = "y1"
        };

        foreach (var value in obj)
        {
            Assert.Equal(typeof(KeyValuePair<string, JsonValue>), value.GetType());
            Assert.Equal(value.Value, obj[value.Key]);
        }

        IEnumerable<JsonValue> genericEnumerable = obj;
        foreach (var value in genericEnumerable)
        {
            Assert.True(obj.Contains(value));
        }

        IEnumerable legacyEnumerable = obj;
        foreach (var value in legacyEnumerable)
        {
            Assert.IsType<KeyValuePair<string, JsonValue>>(value);
            Assert.True(obj.Contains(((KeyValuePair<string, JsonValue>)value).Value));
        }
    }

    [Fact]
    public void TestRename()
    {
        var obj = new JsonObject { ["x"] = "value1", ["y"] = "value2" };
        Assert.Equal(2, obj.Count);

        var value = obj["x"].AsString;
        Assert.False(obj.ContainsKey("z"));
        Assert.Same(obj, obj.Rename("x", "z"));
        Assert.Same(value, obj["z"].AsString);
        Assert.False(obj.ContainsKey("x"));
        Assert.Equal(2, obj.Count);

        // Renaming can overwrite a value
        Assert.Same(obj, obj.Rename("z", "y"));
        Assert.Same(value, obj["y"].AsString);
        Assert.Equal(1, obj.Count);

        // Renaming to the same name does nothing
        Assert.Same(obj, obj.Rename("y", "y"));
        Assert.Same(value, obj["y"].AsString);
        Assert.Equal(1, obj.Count);

        // Renaming a non-existent element is not a problem, and does not overwrite the target
        Assert.Same(obj, obj.Rename("bogus", "y"));
        Assert.Equal(1, obj.Count);
        Assert.Same(value, obj["y"].AsString);
    }
}
