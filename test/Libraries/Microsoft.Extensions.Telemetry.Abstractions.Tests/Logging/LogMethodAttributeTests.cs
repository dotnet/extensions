// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Logging.Test;

public class LogMethodAttributeTests
{
    [Fact]
    public void Basic()
    {
        var a = new LogMethodAttribute(42, LogLevel.Trace, "Foo");
        Assert.Equal(42, a.EventId);
        Assert.Equal(LogLevel.Trace, a.Level);
        Assert.Equal("Foo", a.Message);
        Assert.Null(a.EventName);
        Assert.False(a.SkipEnabledCheck);

        a.EventName = "Name";
        Assert.Equal("Name", a.EventName);

        a.SkipEnabledCheck = true;
        Assert.True(a.SkipEnabledCheck);

        a = new LogMethodAttribute(42, "Foo");
        Assert.Equal(42, a.EventId);
        Assert.False(a.Level.HasValue);
        Assert.Equal("Foo", a.Message);
        Assert.Null(a.EventName);

        a.EventName = "Name";
        Assert.Equal("Name", a.EventName);

        a = new LogMethodAttribute("Foo");
        Assert.Equal(0, a.EventId);
        Assert.False(a.Level.HasValue);
        Assert.Equal("Foo", a.Message);
        Assert.Null(a.EventName);

        a = new LogMethodAttribute(LogLevel.Debug);
        Assert.Equal(0, a.EventId);
        Assert.Equal(LogLevel.Debug, a.Level!.Value);
        Assert.Equal(string.Empty, a.Message);
        Assert.Null(a.EventName);

        a = new LogMethodAttribute(LogLevel.Debug, "Foo");
        Assert.Equal(0, a.EventId);
        Assert.Equal(LogLevel.Debug, a.Level!.Value);
        Assert.Equal("Foo", a.Message);
        Assert.Null(a.EventName);

        a = new LogMethodAttribute(123);
        Assert.Equal(123, a.EventId);
        Assert.False(a.Level.HasValue);
        Assert.Equal(string.Empty, a.Message);
        Assert.Null(a.EventName);

        a = new LogMethodAttribute(123, "Foo");
        Assert.Equal(123, a.EventId);
        Assert.False(a.Level.HasValue);
        Assert.Equal("Foo", a.Message);
        Assert.Null(a.EventName);

        a = new LogMethodAttribute(123, LogLevel.Debug);
        Assert.Equal(123, a.EventId);
        Assert.Equal(LogLevel.Debug, a.Level!.Value);
        Assert.Equal(string.Empty, a.Message);
        Assert.Null(a.EventName);

        a = new LogMethodAttribute();
        Assert.Equal(0, a.EventId);
        Assert.Equal(string.Empty, a.Message);
        Assert.Null(a.Level);
        Assert.Null(a.EventName);
    }
}
