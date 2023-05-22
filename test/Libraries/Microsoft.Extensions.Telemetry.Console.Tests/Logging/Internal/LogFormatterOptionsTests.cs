// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET5_0_OR_GREATER
using Xunit;

namespace Microsoft.Extensions.Telemetry.Console.Internal.Test;

public class LogFormatterOptionsTests
{
    private readonly LogFormatterOptions _testClass;

    public LogFormatterOptionsTests()
    {
        _testClass = new LogFormatterOptions();
    }

    [Fact]
    public void CanConstruct()
    {
        var instance = new LogFormatterOptions();
        Assert.NotNull(instance);
    }

    [Fact]
    public void CanSetAndGetIncludeTimestamp()
    {
        const bool TestValue = true;
        _testClass.IncludeTimestamp = TestValue;
        Assert.Equal(TestValue, _testClass.IncludeTimestamp);
    }

    [Fact]
    public void CanSetAndGetIncludeLogLevel()
    {
        const bool TestValue = true;
        _testClass.IncludeLogLevel = TestValue;
        Assert.Equal(TestValue, _testClass.IncludeLogLevel);
    }

    [Fact]
    public void CanSetAndGetIncludeCategory()
    {
        const bool TestValue = true;
        _testClass.IncludeCategory = TestValue;
        Assert.Equal(TestValue, _testClass.IncludeCategory);
    }

    [Fact]
    public void CanSetAndGetIncludeExceptionStacktrace()
    {
        const bool TestValue = true;
        _testClass.IncludeExceptionStacktrace = TestValue;
        Assert.Equal(TestValue, _testClass.IncludeExceptionStacktrace);
    }

    [Fact]
    public void CheckDefaultLogFormatterOptions()
    {
        var options = new LogFormatterOptions();
        Assert.True(options.IncludeScopes);
        Assert.Equal("yyyy-MM-dd HH:mm:ss.fff", options.TimestampFormat);
        Assert.False(options.UseUtcTimestamp);
        Assert.True(options.IncludeTimestamp);
        Assert.True(options.IncludeLogLevel);
        Assert.True(options.IncludeCategory);
        Assert.True(options.IncludeExceptionStacktrace);
    }

    [Fact]
    public void CheckDefaultLogFormatterTheme()
    {
        var theme = new LogFormatterTheme();
        Assert.True(theme.ColorsEnabled);
    }
}
#endif
