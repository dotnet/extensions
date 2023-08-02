// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using TestClasses;
using Xunit;

namespace Microsoft.Gen.Logging.Test;

public class LogMethodAttributeTests
{
    [Fact]
    public void AllowsAttributesStaticMethod()
    {
        using var logger = Utils.GetLogger();
        var collector = logger.FakeLogCollector;

        collector.Clear();
        AttributeTestExtensions.M0(logger, "arg0");
        Assert.Null(collector.LatestRecord.Exception);
        Assert.Equal("M0 arg0", collector.LatestRecord.Message);
        Assert.Equal(1, collector.Count);
    }

    [Fact]
    public void AllowsAttributesInstanceMethod()
    {
        using var logger = Utils.GetLogger();
        var collector = logger.FakeLogCollector;

        collector.Clear();
        new NonStaticTestClass(logger).M0("arg0");
        Assert.Null(collector.LatestRecord.Exception);
        Assert.Equal("M0 arg0", collector.LatestRecord.Message);
        Assert.Equal(1, collector.Count);
    }

    [Fact]
    public void RedactsArgumentsOnlyWithDataClassificationAttributes()
    {
        using var logger = Utils.GetLogger();
        var collector = logger.FakeLogCollector;

        collector.Clear();
        AttributeTestExtensions.M1(logger, "arg0", "arg1");
        Assert.Null(collector.LatestRecord.Exception);
        Assert.Equal("M1 **** arg1", collector.LatestRecord.Message);
        Assert.Equal(1, collector.Count);

        collector.Clear();
        AttributeTestExtensions.M2(logger, "arg0", "arg1");
        Assert.Null(collector.LatestRecord.Exception);
        Assert.Equal("M2 **** arg1", collector.LatestRecord.Message);
        Assert.Equal(1, collector.Count);
    }

    [Fact]
    public void RedactsArgumentsWithToString()
    {
        using var logger = Utils.GetLogger();
        var collector = logger.FakeLogCollector;

        collector.Clear();
        AttributeTestExtensions.M8(logger, 123456);
        Assert.Null(collector.LatestRecord.Exception);
        Assert.Equal("M8 ******", collector.LatestRecord.Message);
        Assert.Equal(1, collector.Count);

        collector.Clear();
        AttributeTestExtensions.M9(logger, new CustomToStringTestClass());
        Assert.Null(collector.LatestRecord.Exception);
        Assert.Equal("M9 ****", collector.LatestRecord.Message);
        Assert.Equal(1, collector.Count);
    }

    [Fact]
    public void HandlesAvailableDataClassificationAttributes()
    {
        using var logger = Utils.GetLogger();
        var collector = logger.FakeLogCollector;

        collector.Clear();
        AttributeTestExtensions.M3(logger, "arg0", "arg1", "arg2", "arg3");
        Assert.Null(collector.LatestRecord.Exception);
        Assert.Equal("M3 **** **** **** ****", collector.LatestRecord.Message);
        Assert.Equal(1, collector.Count);

        collector.Clear();
        AttributeTestExtensions.M4(logger, "arg0", "arg1", "arg2");
        Assert.Null(collector.LatestRecord.Exception);
        Assert.Equal("M4 **** **** ****", collector.LatestRecord.Message);
        Assert.Equal(1, collector.Count);
    }

    [Fact]
    public void RedactsWhenExceedsMaxLogMethodDefineArguments()
    {
        using var logger = Utils.GetLogger();
        var collector = logger.FakeLogCollector;

        collector.Clear();
        AttributeTestExtensions.M5(logger, "arg0", "arg1", "arg2", "arg3", "arg4", "arg5", "arg6", "arg7", "arg8", "arg9", "arg10");
        Assert.Null(collector.LatestRecord.Exception);
        Assert.Equal("M5 **** **** **** **** **** **** **** **** **** **** *****", collector.LatestRecord.Message);
        Assert.Equal(1, collector.Count);
    }

    [Fact]
    public void RedactsWhenDefaultLogMethodCtor()
    {
        using var logger = Utils.GetLogger();
        var collector = logger.FakeLogCollector;

        collector.Clear();
        AttributeTestExtensions.M6(logger, LogLevel.Critical, "arg0", "arg1");
        AssertWhenDefaultLogMethodCtor(collector, LogLevel.Critical, ("p0", "****"), ("p1", "arg1"));

        collector.Clear();
        AttributeTestExtensions.M7(logger, LogLevel.Warning, "arg_0", "arg_1");
        AssertWhenDefaultLogMethodCtor(collector, LogLevel.Warning, ("p0", "*****"), ("p1", "arg_1"));
    }

    [Fact]
    public void RedactsWhenRedactorProviderIsAvailableInTheInstance()
    {
        using var logger = Utils.GetLogger();
        var collector = logger.FakeLogCollector;

        collector.Clear();
        new NonStaticTestClass(logger).M1("arg0");
        Assert.Null(collector.LatestRecord.Exception);
        Assert.Equal("M1 ****", collector.LatestRecord.Message);
        Assert.Equal(1, collector.Count);

        collector.Clear();
        new NonStaticTestClass(logger).M2("arg0", "arg1", "arg2");
        Assert.Null(collector.LatestRecord.Exception);
        Assert.Equal("M2 **** **** ****", collector.LatestRecord.Message);
        Assert.Equal(1, collector.Count);

        collector.Clear();
        new NonStaticTestClass(logger).M3(LogLevel.Information, "arg_0");
        AssertWhenDefaultLogMethodCtor(collector, LogLevel.Information, ("p0", "*****"));
    }

    private static void AssertWhenDefaultLogMethodCtor(FakeLogCollector collector, LogLevel expectedLevel, params (string key, string value)[] expectedState)
    {
        Assert.Equal(1, collector.Count);
        Assert.Null(collector.LatestRecord.Exception);
        Assert.Equal(string.Empty, collector.LatestRecord.Message);
        Assert.Equal(expectedLevel, collector.LatestRecord.Level);
        Assert.NotNull(collector.LatestRecord.StructuredState);
        Assert.Equal(expectedState.Length, collector.LatestRecord.StructuredState!.Count);
        foreach ((string key, string value) in expectedState)
        {
            Assert.Contains(collector.LatestRecord.StructuredState, x => x.Key == key && x.Value == value);
        }
    }
}
