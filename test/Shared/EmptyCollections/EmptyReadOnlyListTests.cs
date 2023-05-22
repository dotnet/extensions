// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Microsoft.Shared.Collections.Test;

public static class EmptyReadOnlyListTests
{
    [Fact]
    public static void InstanceTests()
    {
        // Verify multiple invocations of Instance return the same object instance
        Assert.Same(EmptyReadOnlyList<int>.Instance, EmptyReadOnlyList<int>.Instance);

        var instance = EmptyReadOnlyList<int>.Instance;

        // Verify multiple invocations of GetEnumerator return the same object instance
        Assert.Same(instance.GetEnumerator(), instance.GetEnumerator());
        Assert.Same(instance.GetEnumerator(), instance.GetEnumerator());
        Assert.Same(((IEnumerable)instance).GetEnumerator(), ((IEnumerable)instance).GetEnumerator());

        instance.Count.Should().Be(0);
        Assert.Throws<ArgumentOutOfRangeException>(() => instance[0]);

        bool enumerated = false;
        foreach (var i in EmptyReadOnlyList<int>.Instance)
        {
            enumerated = true;
        }

        enumerated.Should().BeFalse();
    }

    [Fact]
    public static void EnumeratorTests()
    {
        var enumerator = EmptyReadOnlyList<int>.Instance.GetEnumerator();
        enumerator.MoveNext().Should().BeFalse();
        enumerator.Reset(); // should not throw
        enumerator.Dispose(); // should not throw, nop method.
        enumerator.MoveNext().Should().BeFalse();

        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.Throws<InvalidOperationException>(() => ((IEnumerator)enumerator).Current);
    }

    [Fact]
    public static void ICollection()
    {
        var coll = EmptyReadOnlyList<int>.Instance as ICollection<int>;

        Assert.Throws<NotSupportedException>(() => coll.Add(1));
        Assert.False(coll.Remove(1));
        Assert.False(coll.Contains(1));
        Assert.True(coll.IsReadOnly);

        // nop
        coll.Clear();
        coll.CopyTo(Array.Empty<int>(), 0);
    }
}
