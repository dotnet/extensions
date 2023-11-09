// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Diagnostics.Latency.Internal;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Latency.Test.Internal;

public class RegistryTests
{
    [Fact]
    public void Registry_NullSet()
    {
        Assert.Throws<ArgumentNullException>(() => new Registry(null!, true));
        Assert.Throws<ArgumentNullException>(() => new Registry(null!, false));
    }

    [Fact]
    public void Registry_SetWithNullValue()
    {
        var s = new[] { "a", null };
        Assert.Throws<ArgumentException>(() => new Registry(s!, true));
        Assert.Throws<ArgumentException>(() => new Registry(s!, false));
    }

    [Fact]
    public void Registry_EmptySet()
    {
        RegistryTests.TestWithEmptySet(true);
        RegistryTests.TestWithEmptySet(false);
    }

    private static void TestWithEmptySet(bool throwOnUnregistered)
    {
        var r = new Registry(Array.Empty<string>(), throwOnUnregistered);
        Assert.True(r.KeyCount == 0);
        Assert.Throws<ArgumentNullException>(() => r.GetRegisteredKeyIndex(null!));
    }

    [Fact]
    public void Registry_SameOrderForKeys()
    {
        var r1 = new Registry(new[] { "a", "b", "c", "d" }, true);
        var ok1 = r1.OrderedKeys;
        var r2 = new Registry(new[] { "a", "b", "c", "d" }, true);
        var ok2 = r2.OrderedKeys;
        for (int i = 0; i < ok1.Length; i++)
        {
            Assert.Equal(ok1[i], ok2[i]);
        }
    }

    [Fact]
    public void Registry_NonEmptySet()
    {
        var r = new Registry(new[] { "a", "b", "c", "d" }, true);
        var ok = r.OrderedKeys;
        Assert.True(ok.Length == 4);
        var o = r.GetRegisteredKeyIndex("c");

        for (int i = 0; i < ok.Length; i++)
        {
            if (ok[i] == "c")
            {
                Assert.True(o == i);
            }
        }
    }

    [Fact]
    public void Registry_ThrowMode_ThrowsOnUnregisteredKey()
    {
        var r = new Registry(new[] { "a", "b", "c", "d" }, true);
        var ok = r.OrderedKeys;
        Assert.True(ok.Length == 4);
        r.GetRegisteredKeyIndex("a");
        Assert.Throws<ArgumentException>(() => r.GetRegisteredKeyIndex("e"));
    }

    [Fact]
    public void Registry_NonThrowMode_DoesNotThrow()
    {
        var r = new Registry(new[] { "a", "b", "c", "d" }, false);
        var ok = r.OrderedKeys;
        Assert.True(ok.Length == 4);
        r.GetRegisteredKeyIndex("a");
        Assert.True(r.GetRegisteredKeyIndex("e") == -1);
    }
}
