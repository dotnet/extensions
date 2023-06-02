// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET5_0_OR_GREATER
using System;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Console.Test;

public class LoggingConsoleOptionsTests
{
    private const string DefaultTimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
    private const ConsoleColor DefaultDimmedTextColor = ConsoleColor.DarkGray;
    private const ConsoleColor DefaultExceptionTextColor = ConsoleColor.Red;
    private const ConsoleColor DefaultExceptionStackTraceTextColor = ConsoleColor.DarkRed;
    private const ConsoleColor DefaultDimensionsTextColor = ConsoleColor.DarkGreen;

    [Fact]
    public void TestDefaultOptions()
    {
        var defaultOptions = new LoggingConsoleOptions();

        Assert.True(defaultOptions.IncludeScopes);
        Assert.True(defaultOptions.IncludeExceptionStacktrace);
        Assert.True(defaultOptions.IncludeLogLevel);
        Assert.True(defaultOptions.IncludeCategory);
        Assert.True(defaultOptions.IncludeTimestamp);
        Assert.True(defaultOptions.IncludeTraceId);
        Assert.True(defaultOptions.IncludeSpanId);
        Assert.False(defaultOptions.UseUtcTimestamp);
        Assert.False(defaultOptions.IncludeDimensions);

        Assert.True(defaultOptions.ColorsEnabled);

        Assert.Equal(DefaultDimmedTextColor, defaultOptions.DimmedColor);
        Assert.Null(defaultOptions.DimmedBackgroundColor);

        Assert.Equal(DefaultExceptionStackTraceTextColor, defaultOptions.ExceptionStackTraceColor);
        Assert.Null(defaultOptions.ExceptionStackTraceBackgroundColor);

        Assert.Equal(DefaultExceptionTextColor, defaultOptions.ExceptionColor);
        Assert.Null(defaultOptions.ExceptionBackgroundColor);

        Assert.Equal(DefaultDimensionsTextColor, defaultOptions.DimensionsColor);
        Assert.Null(defaultOptions.DimensionsBackgroundColor);

        Assert.Equal(DefaultTimestampFormat, defaultOptions.TimestampFormat);
    }
}
#endif
