// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Shared.Collections.Test;

public static class EmptyReadOnlyDictionaryTests
{
    [Fact]
    public static void InstanceTest()
    {
        EmptyReadOnlyDictionary<int, string> instance = EmptyReadOnlyDictionary<int, string>.Instance;

        Assert.Throws<KeyNotFoundException>(() => instance[5]);

        Assert.Equal(EmptyReadOnlyList<int>.Instance, instance.Keys);
        Assert.Equal(EmptyReadOnlyList<string>.Instance, instance.Values);

#pragma warning disable xUnit2013 // Need to test count.
        Assert.Equal(0, instance.Count);
#pragma warning restore xUnit2013

        Assert.False(instance.ContainsKey(5));
        Assert.False(instance.TryGetValue(5, out _));

        Assert.Empty(instance);
    }

    [Fact]
    public static void IDictionary()
    {
        var dict = EmptyReadOnlyDictionary<int, string>.Instance as IDictionary<int, string>;

        Assert.Throws<NotSupportedException>(() => dict.Add(1, "One"));
        Assert.False(dict.Remove(1));
        Assert.False(dict.Contains(new KeyValuePair<int, string>(1, "One")));
        Assert.False(dict.ContainsKey(1));
        Assert.True(dict.IsReadOnly);
        Assert.Empty(dict.Keys);
        Assert.Empty(dict.Values);
        Assert.False(dict.TryGetValue(1, out string? value));
        Assert.Null(value);
        Assert.Throws<KeyNotFoundException>(() => dict[1]);
        Assert.Throws<NotSupportedException>(() => dict[1] = "One");
        Assert.Throws<NotSupportedException>(() => dict.Add(1, "One"));

        // nop
        dict.Clear();
        dict.CopyTo(Array.Empty<KeyValuePair<int, string>>(), 0);
    }

    [Fact]
    public static void ICollection()
    {
        var coll = EmptyReadOnlyDictionary<int, string>.Instance as ICollection<KeyValuePair<int, string>>;

        Assert.Throws<NotSupportedException>(() => coll.Add(new KeyValuePair<int, string>(1, "One")));
        Assert.False(coll.Remove(new KeyValuePair<int, string>(1, "One")));
        Assert.False(coll.Contains(new KeyValuePair<int, string>(1, "One")));
    }
}
