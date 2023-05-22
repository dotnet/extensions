// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET5_0_OR_GREATER
using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Console.Internal.Test;

public class ColorSetTests
{
    [Theory]
    [MemberData(nameof(DifferentSetsData))]
    internal void Equals_WhenComparingTwoDifferent_ReturnsFalse(ColorSet color1, ColorSet color2)
    {
        Assert.NotEqual(color1, color2);
        Assert.NotEqual(color1.GetHashCode(), color2.GetHashCode());

        Assert.False(color1 == color2);
        Assert.False(color1.Equals((object)color2));
        Assert.True(color1 != color2);
    }

    [Fact]
    public void Equals_WhenComparingTwoIdentical_ReturnsTrue()
    {
        var color1 = Colors.BlueOnNone;
        var color2 = Colors.BlueOnNone;

        Assert.Equal(color1, color2);
        Assert.Equal(color1.GetHashCode(), color2.GetHashCode());

        Assert.True(color1 == color2);
        Assert.True(color1.Equals((object)color2));
        Assert.False(color1 != color2);
    }

    [Fact]
    public void Equals_WhenComparingWithDifferentTypes_ReturnsFalse()
    {
        var color = Colors.BlackOnCyan;
        Assert.False(color.Equals(null));
        Assert.False(color.Equals(string.Empty));
        Assert.False(color.Equals(new object()));
    }

    [Theory]
    [InlineData(ConsoleColor.Red, ConsoleColor.Green, "Red on Green")]
    [InlineData(ConsoleColor.White, ConsoleColor.Blue, "White on Blue")]
    [InlineData(null, ConsoleColor.Green, "None on Green")]
    [InlineData(ConsoleColor.Red, null, "Red on None")]
    [InlineData(null, null, "None on None")]
    public void ToString_WhenCalledOnDifferentColors_ReturnsCorrect(ConsoleColor? foreground, ConsoleColor? background, string expected)
    {
        var colorSet = new ColorSet(foreground, background);
        Assert.Equal(expected, colorSet.ToString());
    }

    public static IEnumerable<object[]> DifferentSetsData =>
        new[]
        {
                new object[] { Colors.BlackOnBlue, Colors.RedOnCyan },
                new object[] { Colors.BlackOnBlue, Colors.BlackOnCyan },
                new object[] { Colors.RedOnBlack, Colors.BlueOnBlack },
                new object[] { Colors.WhiteOnCyan, Colors.YellowOnWhite }
        };
}
#endif
