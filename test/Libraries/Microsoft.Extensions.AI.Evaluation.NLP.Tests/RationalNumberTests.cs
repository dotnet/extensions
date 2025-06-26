// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.AI.Evaluation.NLP.Common;
using Xunit;

namespace Microsoft.Extensions.AI.Evaluation.NLP.Tests;

public class RationalNumberTests
{
    [Fact]
    public void Constructor_StoresNumeratorAndDenominator()
    {
        var r = new RationalNumber(3, 4);
        Assert.Equal(3, r.Numerator);
        Assert.Equal(4, r.Denominator);
    }

    [Fact]
    public void Constructor_ThrowsOnZeroDenominator()
    {
        Assert.Throws<DivideByZeroException>(() => new RationalNumber(1, 0));
    }

    [Theory]
    [InlineData(1, 2, 0.5)]
    [InlineData(-3, 4, -0.75)]
    [InlineData(0, 5, 0.0)]
    public void ToDouble_ReturnsExpected(int num, int denom, double expected)
    {
        var r = new RationalNumber(num, denom);
        Assert.Equal(expected, r.ToDouble(), 6);
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        var r = new RationalNumber(7, 9);
        Assert.Equal("7/9", r.ToDebugString());
    }

    [Fact]
    public void Equals_And_HashCode_WorkCorrectly()
    {
        var a = new RationalNumber(2, 3);
        var b = new RationalNumber(2, 3);
        var c = new RationalNumber(3, 2);
        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
        Assert.False(a.Equals(c));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a.GetHashCode(), c.GetHashCode());
    }
}
