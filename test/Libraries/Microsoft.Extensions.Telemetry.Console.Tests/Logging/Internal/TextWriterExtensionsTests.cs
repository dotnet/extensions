// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET5_0_OR_GREATER
using System;
using System.IO;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Console.Internal.Test;

/// <summary>
/// Test cases for <see cref="TextWriterExtensions"/>.
/// </summary>
/// <remarks>
/// Colorization produces strings that contain something like <c>[1m[35m[49m</c>
/// - those characters are special escape sequence to enable colouring in a console.
/// </remarks>
public class TextWriterExtensionsTests
{
    [Fact]
    public void Colorize_WhenSimpleExpandInMiddleOfText_IsCorrect()
    {
        using var writer = new StringWriter();

        writer.Colorize("Text before {{0}} text after", Colors.BlackOnBlue, "colorized");
        const string Expected = "Text before [30m[49mcolorized[49m[39m[22m text after";

        Assert.Equal(Expected, writer.ToString());
    }

    [Fact]
    public void Colorize_WhenExpandInTheBeginning_IsCorrect()
    {
        using var writer = new StringWriter();

        writer.Colorize("{{0}} text after", Colors.BlackOnBlue, "colorized");
        const string Expected = "[30m[49mcolorized[49m[39m[22m text after";

        Assert.Equal(Expected, writer.ToString());
    }

    [Fact]
    public void Colorize_WhenExpandInTheEnd_IsCorrect()
    {
        using var writer = new StringWriter();

        writer.Colorize("Text before {{0}}", Colors.BlackOnBlue, "colorized");
        const string Expected = "Text before [30m[49mcolorized[49m[39m[22m";

        Assert.Equal(Expected, writer.ToString());
    }

    [Fact]
    public void Colorize_WhenUsedWithoutExpandParameter_IsCorrect()
    {
        using var writer = new StringWriter();

        writer.Colorize("Text before {colorized} text after", Colors.BlackOnBlue);
        const string Expected = "Text before [30m[49mcolorized[49m[39m[22m text after";

        Assert.Equal(Expected, writer.ToString());
    }

    [Fact]
    public void WriteCoordinate_WhenCalledWithEnumerable_IsCorrect()
    {
        using var writer = new StringWriter();
        writer.WriteCoordinate(new[] { 1, 2, 3 }, Colors.GrayOnBlack);
        string expected = Environment.NewLine + "[37m[40m1:2:3:[49m[39m[22m ";

        Assert.Equal(expected, writer.ToString());
    }

    [Theory]
    [InlineData("{{{e{xpanding}}", "e[1m[35m[49mxpanding}[49m[39m[22m")]
    [InlineData("test {", "test ")]
    [InlineData("{} test", " test")]
    [InlineData("{0}", "")]
    [InlineData("{0} {1f}", " f}")]
    [InlineData("{2test}", "test}")]
    [InlineData("{test}", "[1m[35m[49mtest[49m[39m[22m")]
    public void Colorize_WhenUsedWithInvalidTemplate_IsCorrect(string format, string expected)
    {
        using var writer = new StringWriter();
        writer.Colorize(format, Colors.MagentaOnWhite);

        Assert.Equal(expected, writer.ToString());
    }
}
#endif
