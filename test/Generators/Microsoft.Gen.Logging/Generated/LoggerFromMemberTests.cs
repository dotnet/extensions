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

    [Fact]
    public void LoggerInProtectedFieldTestClass()
    {
        var logger = new FakeLogger<int>();
        var collector = logger.Collector;

        var o = new LoggerInProtectedFieldTestClass(logger);

        o.M0("arg0");
        Assert.Null(collector.LatestRecord.Exception);
        Assert.Equal("M0 arg0", collector.LatestRecord.Message);
        Assert.Equal(1, collector.Count);
    }

    [Fact]
    public void PrivateLoggerInNullablePropertyDerivedTestClass()
    {
        var logger = new FakeLogger<int>();

        var o = new PrivateLoggerInNullablePropertyDerivedTestClass(logger);

        o.M0("arg0");
        Assert.Equal(0, logger.Collector.Count);

        o.M1("arg0");
        Assert.Null(logger.Collector.LatestRecord.Exception);
        Assert.Equal("M1 arg0", logger.Collector.LatestRecord.Message);
        Assert.Equal(1, logger.Collector.Count);

        var logger2 = new FakeLogger<int>();

        o.Logger = logger2;

        o.M0("arg1");
        o.M1("arg1");
        Assert.Null(logger.Collector.LatestRecord.Exception);
        Assert.Equal("M1 arg1", logger.Collector.LatestRecord.Message);
        Assert.Equal(2, logger.Collector.Count);
        Assert.Null(logger2.Collector.LatestRecord.Exception);
        Assert.Equal("M0 arg1", logger2.Collector.LatestRecord.Message);
        Assert.Equal(1, logger2.Collector.Count);
    }

    [Fact]
    public void LoggerInProtectedFieldDerivedTestClass()
    {
        var logger = new FakeLogger<int>();
        var collector = logger.Collector;

        var o = new LoggerInProtectedFieldDerivedTestClass(logger);

        o.M0("arg0");
        Assert.Null(collector.LatestRecord.Exception);
        Assert.Equal("M0 arg0", collector.LatestRecord.Message);
        Assert.Equal(1, collector.Count);
    }

    [Fact]
    public void GenericLoggerInPropertyDerivedTestClass()
    {
        var logger = new FakeLogger<int>();
        var collector = logger.Collector;

        var o = new GenericLoggerInPropertyDerivedTestClass
        {
            Logger = logger
        };

        o.M0("arg0");
        Assert.Null(collector.LatestRecord.Exception);
        Assert.Equal("M0 arg0", collector.LatestRecord.Message);
        Assert.Equal(1, collector.Count);

        o.M1("arg0");
        Assert.Null(collector.LatestRecord.Exception);
        Assert.Equal("M1 arg0", collector.LatestRecord.Message);
        Assert.Equal(2, collector.Count);
    }
}
