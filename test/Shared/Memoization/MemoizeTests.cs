// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Shared.Memoization.Test;

public class MemoizeTests
{
    [Fact]
    public void MemoizeFunction_Arity1_CanInvoke()
    {
        Func<int, int> doubler = x => x * 2;
        var memoized = Memoize.Function(doubler);
        Assert.Equal(4, memoized(2));
        Assert.Equal(6, memoized(3));
    }

    [Fact]
    public async Task MemoizeFunction_TaskReturningMethod_CanInvoke()
    {
        Func<int, Task<int>> doubler = x => Task.FromResult(x * 2);
        var memoized = Memoize.Function(doubler);
        Assert.Equal(4, await memoized(2));
        Assert.Equal(6, await memoized(3));
    }

    [Fact]
    public void MemoizeFunction_Arity2_CanInvoke()
    {
        Func<int, int, int> adder = (x, y) => x + y;
        var memoized = Memoize.Function(adder);
        Assert.Equal(4, memoized(2, 2));
        Assert.Equal(8, memoized(3, 5));
    }

    [Fact]
    public void MemoizeFunction_Arity3_CanInvoke()
    {
        Func<int, int, int, int> adder = (x, y, z) => x + y + z;
        var memoized = Memoize.Function(adder);
        Assert.Equal(6, memoized(1, 2, 3));
        Assert.Equal(9, memoized(3, 5, 1));
    }

    [Fact]
    public void MemoizeFunctionArity1_InvokedMultipleTimes_InvokesFunctionOnlyOnce()
    {
        var callCount = 0;
        Func<int, int> doubler = x =>
        {
            callCount++;
            return x * 2;
        };
        var memoized = Memoize.Function(doubler);
        Assert.Equal(4, memoized(2));
        Assert.Equal(4, memoized(2));
        Assert.Equal(1, callCount);

        Assert.Equal(6, memoized(3));
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void MemoizeFunctionArity1_InvokedMultipleTimesWithNull_InvokesFunctionOnlyOnce()
    {
        var callCount = 0;
        Func<object?, string> toString = x =>
        {
            callCount++;
            return x?.ToString() ?? "null";
        };

        var memoized = Memoize.Function(toString);
        Assert.Equal("null", memoized(null));
        Assert.Equal("null", memoized(null));
        Assert.Equal(1, callCount);

        Assert.Equal("3", memoized(3));
        Assert.Equal(2, callCount);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    public void MemoizeFunctionArity2_InvokedMultipleTimes_InvokesFunctionOnlyOnce(int a, int b)
    {
        var callCount = 0;
        Func<int, int, int> adder = (x, y) =>
        {
            callCount++;
            return x + y;
        };
        var memoized = Memoize.Function(adder);
        Assert.Equal(a + b, memoized(a, b));
        Assert.Equal(a + b, memoized(a, b));
        Assert.Equal(1, callCount);

        Assert.Equal(a + b + 1, memoized(a, b + 1));
        Assert.Equal(2, callCount);
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData(0, null)]
    public void MemoizeFunctionArity2_InvokedMultipleTimesWithNull_InvokesFunctionOnlyOnce(int? a, int? b)
    {
        var callCount = 0;
        Func<object?, object?, string> toString = (_, _) =>
        {
            callCount++;
            return "return value";
        };

        var memoized = Memoize.Function(toString);
        Assert.Equal("return value", memoized(a, b));
        Assert.Equal("return value", memoized(a, b));
        Assert.Equal(1, callCount);

        Assert.Equal("return value", memoized(a ?? 0 + 1, b ?? 0 + 1));
        Assert.Equal(2, callCount);
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 0, 0)]
    public void MemoizeFunctionArity3_InvokedMultipleTimes_InvokesFunctionOnlyOnce(int a, int b, int c)
    {
        var callCount = 0;
        Func<int, int, int, int> adder = (x, y, z) =>
        {
            callCount++;
            return x + y + z;
        };
        var memoized = Memoize.Function(adder);
        Assert.Equal(a + b + c, memoized(a, b, c));
        Assert.Equal(a + b + c, memoized(a, b, c));
        Assert.Equal(1, callCount);

        Assert.Equal(a + b + c + 1, memoized(a, b, c + 1));
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void Arg1_Equals_Reflexive()
    {
        var a = new MemoizedFunction<int, int>.Arg(0);
        Assert.Equal(a, a);
        Assert.True(a.Equals(a));
        Assert.True(a.Equals((object)a));

        Assert.Equal(a.GetHashCode(), a.GetHashCode());
    }

    [Fact]
    public void Arg1_Equals_Symmetric()
    {
        var a = new MemoizedFunction<int, int>.Arg(0);
        var b = new MemoizedFunction<int, int>.Arg(0);
        Assert.Equal(a, b);
        Assert.Equal(b, a);

        Assert.True(a.Equals(b));
        Assert.True(b.Equals(a));

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.Equal(b.GetHashCode(), a.GetHashCode());
    }

    [Fact]
    public void Arg1_Equals_Transitive()
    {
        var a = new MemoizedFunction<int, int>.Arg(1);
        var b = new MemoizedFunction<int, int>.Arg(1);
        var c = new MemoizedFunction<int, int>.Arg(1);
        Assert.Equal(a, b);
        Assert.Equal(b, c);
        Assert.Equal(c, a);

        Assert.True(a.Equals(b));
        Assert.True(b.Equals(c));
        Assert.True(c.Equals(a));

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.Equal(b.GetHashCode(), c.GetHashCode());
        Assert.Equal(a.GetHashCode(), a.GetHashCode());
    }

    [Fact]
    public void Arg1_Equals_UnequalThingsNotEqual()
    {
        static MemoizedFunction<int, int>.Arg Args(int x) => new(x);
        Assert.NotEqual(Args(0), Args(1));

        Assert.NotEqual(Args(0).GetHashCode(), Args(1).GetHashCode());

        Assert.False(Args(0).Equals(null));
    }

    [Fact]
    public void Arg2_Equals_Reflexive()
    {
        var a = new MemoizedFunction<int, int, int>.Args(0, 0);
        Assert.Equal(a, a);
        Assert.True(a.Equals(a));
        Assert.True(a.Equals((object?)a));

        Assert.Equal(a.GetHashCode(), a.GetHashCode());
    }

    [Fact]
    public void Arg2_Equals_Symmetric()
    {
        var a = new MemoizedFunction<int, int, int>.Args(0, 0);
        var b = new MemoizedFunction<int, int, int>.Args(0, 0);
        Assert.Equal(a, b);
        Assert.Equal(b, a);

        Assert.True(a.Equals(b));
        Assert.True(b.Equals(a));

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.Equal(b.GetHashCode(), a.GetHashCode());
    }

    [Fact]
    public void Arg2_Equals_Transitive()
    {
        var a = new MemoizedFunction<int, int, int>.Args(1, 1);
        var b = new MemoizedFunction<int, int, int>.Args(1, 1);
        var c = new MemoizedFunction<int, int, int>.Args(1, 1);
        Assert.Equal(a, b);
        Assert.Equal(b, c);
        Assert.Equal(c, a);

        Assert.True(a.Equals(b));
        Assert.True(b.Equals(c));
        Assert.True(c.Equals(a));

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.Equal(b.GetHashCode(), c.GetHashCode());
        Assert.Equal(a.GetHashCode(), a.GetHashCode());
    }

    [Fact]
    public void Arg2_Equals_UnequalThingsNotEqual()
    {
        static MemoizedFunction<int, int, int>.Args Args(int x, int y) => new(x, y);

        Assert.NotEqual(Args(0, 0), Args(0, 1));
        Assert.NotEqual(Args(0, 0), Args(1, 0));

        Assert.NotEqual(Args(0, 0).GetHashCode(), Args(0, 1).GetHashCode());

        Assert.False(Args(0, 0).Equals(null));
    }

    [Fact]
    public void Arg3_Equals_Reflexive()
    {
        var a = new MemoizedFunction<int, int, int, int>.Args(0, 0, 0);
        Assert.Equal(a, a);
        Assert.True(a.Equals(a));
        Assert.True(a.Equals((object?)a));

        Assert.Equal(a.GetHashCode(), a.GetHashCode());
    }

    [Fact]
    public void Arg3_Equals_Symmetric()
    {
        var a = new MemoizedFunction<int, int, int, int>.Args(0, 0, 0);
        var b = new MemoizedFunction<int, int, int, int>.Args(0, 0, 0);
        Assert.Equal(a, b);
        Assert.Equal(b, a);

        Assert.True(a.Equals(b));
        Assert.True(b.Equals(a));

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.Equal(b.GetHashCode(), a.GetHashCode());
    }

    [Fact]
    public void Arg3_Equals_Transitive()
    {
        var a = new MemoizedFunction<int, int, int, int>.Args(1, 1, 1);
        var b = new MemoizedFunction<int, int, int, int>.Args(1, 1, 1);
        var c = new MemoizedFunction<int, int, int, int>.Args(1, 1, 1);
        Assert.Equal(a, b);
        Assert.Equal(b, c);
        Assert.Equal(c, a);

        Assert.True(a.Equals(b));
        Assert.True(b.Equals(c));
        Assert.True(c.Equals(a));

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.Equal(b.GetHashCode(), c.GetHashCode());
        Assert.Equal(a.GetHashCode(), a.GetHashCode());
    }

    [Fact]
    public void Arg3_Equals_UnequalThingsNotEqual()
    {
        static MemoizedFunction<int, int, int, int>.Args Args(int x, int y, int z) => new(x, y, z);

        Assert.NotEqual(Args(0, 0, 0), Args(1, 0, 0));
        Assert.NotEqual(Args(0, 0, 0), Args(0, 1, 0));
        Assert.NotEqual(Args(0, 0, 0), Args(0, 0, 1));

        Assert.NotEqual(Args(0, 0, 0).GetHashCode(), Args(1, 0, 0).GetHashCode());
        Assert.NotEqual(Args(0, 0, 0).GetHashCode(), Args(0, 1, 0).GetHashCode());
        Assert.NotEqual(Args(0, 0, 0).GetHashCode(), Args(0, 0, 1).GetHashCode());

        Assert.False(Args(0, 0, 0).Equals(null));
    }

    [Theory]
    [InlineData(null, 0, 0)]
    [InlineData(0, null, 0)]
    [InlineData(0, 0, null)]
    public void MemoizeFunctionArity3_InvokedMultipleTimesWithNull_InvokesFunctionOnlyOnce(int? a, int? b, int? c)
    {
        var callCount = 0;
        Func<object?, object?, object?, string> toString = (_, _, _) =>
        {
            callCount++;
            return "return value";
        };

        var memoized = Memoize.Function(toString);
        Assert.Equal("return value", memoized(a, b, c));
        Assert.Equal("return value", memoized(a, b, c));
        Assert.Equal(1, callCount);

        Assert.Equal("return value", memoized(a ?? 0 + 1, b ?? 0 + 1, c ?? 0 + 1));
        Assert.Equal(2, callCount);
    }
}
