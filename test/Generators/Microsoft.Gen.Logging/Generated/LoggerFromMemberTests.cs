// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging.Testing;
using TestClasses;
using Xunit;

namespace Microsoft.Gen.Logging.Test;

public class LoggerFromMemberTests
{
    [Fact]
    public void LoggerInPropertyTestClass()
    {
        using var logger = Utils.GetLogger();
        var collector = logger.FakeLogCollector;

        var o = new LoggerInPropertyTestClass
        {
            Logger = logger
        };

        o.M0("arg0");
        Assert.Null(collector.LatestRecord.Exception);
        Assert.Equal("M0 arg0", collector.LatestRecord.Message);
        Assert.Equal(1, collector.Count);
    }

    [Fact]
    public void LoggerInNullablePropertyTestClass()
    {
        using var logger = Utils.GetLogger();
        var collector = logger.FakeLogCollector;

        var o = new LoggerInNullablePropertyTestClass
        {
            Logger = logger
        };

        o.M0("arg0");
        Assert.Null(collector.LatestRecord.Exception);
        Assert.Equal("M0 arg0", collector.LatestRecord.Message);
        Assert.Equal(1, collector.Count);

        o.Logger = null;
        o.M0("arg0");
        Assert.Equal(1, collector.Count);
    }

    [Fact]
    public void LoggerInPropertyDerivedTestClass()
    {
        using var logger = Utils.GetLogger();
        var collector = logger.FakeLogCollector;

        var o = new LoggerInPropertyDerivedTestClass
        {
            Logger = logger
        };

        o.M0("arg0");
        Assert.Null(collector.LatestRecord.Exception);
        Assert.Equal("M0 arg0", collector.LatestRecord.Message);
        Assert.Equal(1, collector.Count);
    }

    [Fact]
    public void LoggerInNullablePropertyDerivedTestClass()
    {
        using var logger = Utils.GetLogger();
        var collector = logger.FakeLogCollector;

        var o = new LoggerInNullablePropertyDerivedTestClass
        {
            Logger = logger
        };

        o.M0("arg0");
        Assert.Null(collector.LatestRecord.Exception);
        Assert.Equal("M0 arg0", collector.LatestRecord.Message);
        Assert.Equal(1, collector.Count);

        o.Logger = null;
        o.M0("arg0");
        Assert.Equal(1, collector.Count);
    }

    [Fact]
    public void GenericLoggerInPropertyTestClass()
    {
        var logger = new FakeLogger<int>();
        var collector = logger.Collector;

        var o = new GenericLoggerInPropertyTestClass
        {
            Logger = logger
        };

        o.M0("arg0");
        Assert.Null(collector.LatestRecord.Exception);
        Assert.Equal("M0 arg0", collector.LatestRecord.Message);
        Assert.Equal(1, collector.Count);
    }
}
