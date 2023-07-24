// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using TestClasses;
using Xunit;

namespace Microsoft.Gen.Logging.Test;

public class LogMethodAttributeTests
{
    private readonly FakeLogger _logger = new();
    private readonly StarRedactorProvider _redactorProvider = new();

    [Fact]
    public void AllowsAttributesStaticMethod()
    {
        _logger.Collector.Clear();
        AttributeTestExtensions.M0(_logger, "arg0");
        Assert.Null(_logger.LatestRecord.Exception);
        Assert.Equal("M0 arg0", _logger.LatestRecord.Message);
        Assert.Equal(1, _logger.Collector.Count);
    }

    [Fact]
    public void AllowsAttributesInstanceMethod()
    {
        _logger.Collector.Clear();
        new NonStaticTestClass(_logger, null!).M0("arg0");
        Assert.Null(_logger.LatestRecord.Exception);
        Assert.Equal("M0 arg0", _logger.LatestRecord.Message);
        Assert.Equal(1, _logger.Collector.Count);
    }

    [Fact]
    public void RedactsArgumentsOnlyWithDataClassificationAttributes()
    {
        _logger.Collector.Clear();
        AttributeTestExtensions.M1(_logger, _redactorProvider, "arg0", "arg1");
        Assert.Null(_logger.LatestRecord.Exception);
        Assert.Equal("M1 **** arg1", _logger.LatestRecord.Message);
        Assert.Equal(1, _logger.Collector.Count);

        _logger.Collector.Clear();
        AttributeTestExtensions.M2(_logger, _redactorProvider, "arg0", "arg1");
        Assert.Null(_logger.LatestRecord.Exception);
        Assert.Equal("M2 **** arg1", _logger.LatestRecord.Message);
        Assert.Equal(1, _logger.Collector.Count);
    }

    [Fact]
    public void RedactsArgumentsWithToString()
    {
        _logger.Collector.Clear();
        AttributeTestExtensions.M8(_logger, _redactorProvider, 123456);
        Assert.Null(_logger.LatestRecord.Exception);
        Assert.Equal("M8 ******", _logger.LatestRecord.Message);
        Assert.Equal(1, _logger.Collector.Count);

        _logger.Collector.Clear();
        AttributeTestExtensions.M9(_logger, _redactorProvider, new CustomToStringTestClass());
        Assert.Null(_logger.LatestRecord.Exception);
        Assert.Equal("M9 ****", _logger.LatestRecord.Message);
        Assert.Equal(1, _logger.Collector.Count);
    }

    [Fact]
    public void HandlesAvailableDataClassificationAttributes()
    {
        _logger.Collector.Clear();
        AttributeTestExtensions.M3(_logger, _redactorProvider, "arg0", "arg1", "arg2", "arg3");
        Assert.Null(_logger.LatestRecord.Exception);
        Assert.Equal("M3 **** **** **** ****", _logger.LatestRecord.Message);
        Assert.Equal(1, _logger.Collector.Count);

        _logger.Collector.Clear();
        AttributeTestExtensions.M4(_logger, _redactorProvider, "arg0", "arg1", "arg2");
        Assert.Null(_logger.LatestRecord.Exception);
        Assert.Equal("M4 **** **** ****", _logger.LatestRecord.Message);
        Assert.Equal(1, _logger.Collector.Count);
    }

    [Fact]
    public void RedactsWhenExceedsMaxLogMethodDefineArguments()
    {
        _logger.Collector.Clear();
        AttributeTestExtensions.M5(_logger, _redactorProvider, "arg0", "arg1", "arg2", "arg3", "arg4", "arg5", "arg6", "arg7", "arg8", "arg9", "arg10");
        Assert.Null(_logger.LatestRecord.Exception);
        Assert.Equal("M5 **** **** **** **** **** **** **** **** **** **** *****", _logger.LatestRecord.Message);
        Assert.Equal(1, _logger.Collector.Count);
    }

    [Fact]
    public void RedactsWhenDefaultLogMethodCtor()
    {
        _logger.Collector.Clear();
        AttributeTestExtensions.M6(_logger, LogLevel.Critical, _redactorProvider, "arg0", "arg1");
        AssertWhenDefaultLogMethodCtor(LogLevel.Critical, ("p0", "****"), ("p1", "arg1"));

        _logger.Collector.Clear();
        AttributeTestExtensions.M7(_logger, LogLevel.Warning, _redactorProvider, "arg_0", "arg_1");
        AssertWhenDefaultLogMethodCtor(LogLevel.Warning, ("p0", "*****"), ("p1", "arg_1"));
    }

    [Fact]
    public void RedactsWhenRedactorProviderIsAvailableInTheInstance()
    {
        _logger.Collector.Clear();
        new NonStaticTestClass(_logger, _redactorProvider).M1("arg0");
        Assert.Null(_logger.LatestRecord.Exception);
        Assert.Equal("M1 ****", _logger.LatestRecord.Message);
        Assert.Equal(1, _logger.Collector.Count);

        _logger.Collector.Clear();
        new NonStaticTestClass(_logger, _redactorProvider).M2("arg0", "arg1", "arg2");
        Assert.Null(_logger.LatestRecord.Exception);
        Assert.Equal("M2 **** **** ****", _logger.LatestRecord.Message);
        Assert.Equal(1, _logger.Collector.Count);

        _logger.Collector.Clear();
        new NonStaticTestClass(_logger, _redactorProvider).M3(LogLevel.Information, "arg_0");
        AssertWhenDefaultLogMethodCtor(LogLevel.Information, ("p0", "*****"));
    }

    private void AssertWhenDefaultLogMethodCtor(LogLevel expectedLevel, params (string key, string value)[] expectedState)
    {
        Assert.Equal(1, _logger.Collector.Count);
        Assert.Null(_logger.LatestRecord.Exception);
        Assert.Equal(string.Empty, _logger.LatestRecord.Message);
        Assert.Equal(expectedLevel, _logger.LatestRecord.Level);
        Assert.NotNull(_logger.LatestRecord.StructuredState);
        Assert.Equal(expectedState.Length, _logger.LatestRecord.StructuredState!.Count);
        foreach ((string key, string value) in expectedState)
        {
            Assert.Contains(_logger.LatestRecord.StructuredState, x => x.Key == key && x.Value == value);
        }
    }
}
