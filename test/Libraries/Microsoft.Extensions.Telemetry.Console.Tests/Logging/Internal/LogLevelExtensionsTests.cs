// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET5_0_OR_GREATER
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Console.Internal.Test;

public class LogLevelExtensionsTests
{
    [Fact]
    public void InShortString_WhenLogLevelTrace()
    {
        const LogLevel Level = LogLevel.Trace;
        Assert.Equal("trce", Level.InShortString());
        Assert.Equal(Colors.GrayOnBlack, Level.InColor());
    }

    [Fact]
    public void InShortString_WhenLogLevelDebug()
    {
        const LogLevel Level = LogLevel.Debug;
        Assert.Equal("dbug", Level.InShortString());
        Assert.Equal(Colors.GrayOnBlack, Level.InColor());
    }

    [Fact]
    public void InShortString_WhenLogLevelInformation()
    {
        const LogLevel Level = LogLevel.Information;
        Assert.Equal("info", Level.InShortString());
        Assert.Equal(Colors.DarkGreenOnBlack, Level.InColor());
    }

    [Fact]
    public void InShortString_WhenLogLevelWarning()
    {
        const LogLevel Level = LogLevel.Warning;
        Assert.Equal("warn", Level.InShortString());
        Assert.Equal(Colors.YellowOnBlack, Level.InColor());
    }

    [Fact]
    public void InShortString_WhenLogLevelError()
    {
        const LogLevel Level = LogLevel.Error;
        Assert.Equal("eror", Level.InShortString());
        Assert.Equal(Colors.BlackOnDarkRed, Level.InColor());
    }

    [Fact]
    public void InShortString_WhenLogLevelCritical()
    {
        const LogLevel Level = LogLevel.Critical;
        Assert.Equal("crit", Level.InShortString());
        Assert.Equal(Colors.WhiteOnDarkRed, Level.InColor());
    }

    [Fact]
    public void InShortString_WhenLogLevelUnrecognized_UsesSafeDefault()
    {
        const LogLevel Level = (LogLevel)999;
        Assert.Equal("none", Level.InShortString());
        Assert.Equal(Colors.None, Level.InColor());
    }

    [Fact]
    public void InColor_WhenLogLevelUnrecognized_UsesSafeDefault()
    {
        const LogLevel Level = (LogLevel)999;
        Assert.Equal(Colors.None, Level.InColor());
    }
}
#endif
