// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test;

public class ByteArrayEqualityComparerTests
{
    private readonly ByteArrayEqualityComparer _comparer = new();

    [Fact]
    public void Equals_ReturnsTrue_WhenBothArraysAreNull()
    {
        Assert.True(_comparer.Equals(null, null));
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenOneArrayIsNull()
    {
        var x = new byte[] { 1, 2, 3 };
        Assert.False(_comparer.Equals(x, null));
        Assert.False(_comparer.Equals(null, x));
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenArraysHaveDifferentLengths()
    {
        var x = new byte[] { 1, 2, 3 };
        var y = new byte[] { 1, 2 };
        Assert.False(_comparer.Equals(x, y));
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenArraysHaveDifferentValues()
    {
        var x = new byte[] { 1, 2, 3 };
        var y = new byte[] { 1, 2, 4 };
        Assert.False(_comparer.Equals(x, y));
    }

    [Fact]
    public void Equals_ReturnsTrue_WhenArraysAreEqual()
    {
        var x = new byte[] { 1, 2, 3 };
        var y = new byte[] { 1, 2, 3 };
        Assert.True(_comparer.Equals(x, y));
    }

    [Fact]
    public void GetHashCode_ReturnsSameValue_ForEqualArrays()
    {
        var x = new byte[] { 1, 2, 3 };
        var y = new byte[] { 1, 2, 3 };
        Assert.Equal(_comparer.GetHashCode(x), _comparer.GetHashCode(y));
    }

    [Fact]
    public void GetHashCode_ReturnsDifferentValue_ForDifferentArrays()
    {
        var x = new byte[] { 1, 2, 3 };
        var y = new byte[] { 1, 2, 4 };
        Assert.NotEqual(_comparer.GetHashCode(x), _comparer.GetHashCode(y));
    }
}

