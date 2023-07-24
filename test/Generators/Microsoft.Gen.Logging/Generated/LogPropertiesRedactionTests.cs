// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using TestClasses;
using Xunit;

using static TestClasses.LogPropertiesRedactionExtensions;

namespace Microsoft.Gen.Logging.Test;

public class LogPropertiesRedactionTests
{
    private readonly FakeLogger _logger = new();
    private readonly StarRedactorProvider _redactorProvider = new();

    [Fact]
    public void RedactsWhenRedactorProviderIsAvailableInTheInstance()
    {
        var instance = new NonStaticTestClass(_logger, _redactorProvider);
        var classToRedact = new MyBaseClassToRedact();

        instance.LogPropertiesWithRedaction("arg0", classToRedact);

        Assert.Equal(1, _logger.Collector.Count);
        Assert.Null(_logger.LatestRecord.Exception);
        Assert.Equal("LogProperties with redaction: ****", _logger.LatestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["P0"] = "****",
            ["p1_StringPropertyBase"] = new('*', classToRedact.StringPropertyBase.Length),
            ["{OriginalFormat}"] = "LogProperties with redaction: {P0}"
        };

        _logger.Collector.LatestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void RedactsWhenDefaultAttrCtorAndRedactorProviderIsInTheInstance()
    {
        var instance = new NonStaticTestClass(_logger, _redactorProvider);
        var classToRedact = new MyBaseClassToRedact();

        instance.DefaultAttrCtorLogPropertiesWithRedaction(LogLevel.Information, "arg0", classToRedact);

        Assert.Equal(1, _logger.Collector.Count);
        Assert.Null(_logger.LatestRecord.Exception);
        Assert.Equal(LogLevel.Information, _logger.LatestRecord.Level);
        Assert.Equal(string.Empty, _logger.LatestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["p0"] = "****",
            ["p1_StringPropertyBase"] = new('*', classToRedact.StringPropertyBase.Length),
        };

        _logger.Collector.LatestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void RedactsWhenLogMethodIsStaticNoParams()
    {
        var classToRedact = new ClassToRedact();

        LogNoParams(_logger, _redactorProvider, classToRedact);

        Assert.Equal(1, _logger.Collector.Count);
        Assert.Null(_logger.LatestRecord.Exception);
        Assert.Equal("No template params", _logger.LatestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["classToLog_StringProperty"] = new('*', classToRedact.StringProperty.Length),
            ["classToLog_StringPropertyBase"] = new('*', classToRedact.StringPropertyBase.Length),
            ["classToLog_SimplifiedNullableIntProperty"] = classToRedact.SimplifiedNullableIntProperty.ToString(CultureInfo.InvariantCulture),
            ["classToLog_GetOnlyProperty"] = new('*', classToRedact.GetOnlyProperty.Length),
            ["classToLog_TransitiveProp_TransitiveNumberProp"] = classToRedact.TransitiveProp.TransitiveNumberProp.ToString(CultureInfo.InvariantCulture),
            ["classToLog_TransitiveProp_TransitiveStringProp"] = new('*', classToRedact.TransitiveProp.TransitiveStringProp.Length),

            ["classToLog_NoRedactionProp"] = classToRedact.NoRedactionProp,
            ["{OriginalFormat}"] = "No template params"
        };

        _logger.Collector.LatestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void RedactsWhenDefaultAttrCtorAndIsStaticNoParams()
    {
        var classToRedact = new ClassToRedact();

        LogNoParamsDefaultCtor(_logger, LogLevel.Warning, _redactorProvider, classToRedact);

        Assert.Equal(1, _logger.Collector.Count);
        Assert.Null(_logger.LatestRecord.Exception);
        Assert.Equal(LogLevel.Warning, _logger.LatestRecord.Level);
        Assert.Equal(string.Empty, _logger.LatestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["classToLog_StringProperty"] = new('*', classToRedact.StringProperty.Length),
            ["classToLog_StringPropertyBase"] = new('*', classToRedact.StringPropertyBase.Length),
            ["classToLog_SimplifiedNullableIntProperty"] = classToRedact.SimplifiedNullableIntProperty.ToString(CultureInfo.InvariantCulture),
            ["classToLog_GetOnlyProperty"] = new('*', classToRedact.GetOnlyProperty.Length),
            ["classToLog_TransitiveProp_TransitiveNumberProp"] = classToRedact.TransitiveProp.TransitiveNumberProp.ToString(CultureInfo.InvariantCulture),
            ["classToLog_TransitiveProp_TransitiveStringProp"] = new('*', classToRedact.TransitiveProp.TransitiveStringProp.Length),

            ["classToLog_NoRedactionProp"] = classToRedact.NoRedactionProp,
        };

        _logger.Collector.LatestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void RedactsWhenLogMethodIsStaticTwoParams()
    {
        var classToRedact = new MyTransitiveClass();

        LogTwoParams(_logger, _redactorProvider, "string_prop", classToRedact);

        Assert.Equal(1, _logger.Collector.Count);
        Assert.Null(_logger.LatestRecord.Exception);
        Assert.Equal("Only *********** as param", _logger.LatestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["StringProperty"] = "***********",
            ["complexParam_TransitiveNumberProp"] = classToRedact.TransitiveNumberProp.ToString(CultureInfo.InvariantCulture),
            ["complexParam_TransitiveStringProp"] = new('*', classToRedact.TransitiveStringProp.Length),
            ["{OriginalFormat}"] = "Only {StringProperty} as param"
        };

        _logger.Collector.LatestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void RedactsWhenDefaultAttrCtorAndIsStaticTwoParams()
    {
        var classToRedact = new MyTransitiveClass();

        LogTwoParamsDefaultCtor(_logger, _redactorProvider, LogLevel.None, "string_prop", classToRedact);

        Assert.Equal(1, _logger.Collector.Count);
        Assert.Null(_logger.LatestRecord.Exception);
        Assert.Equal(LogLevel.None, _logger.LatestRecord.Level);
        Assert.Equal(string.Empty, _logger.LatestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["stringProperty"] = "***********",
            ["complexParam_TransitiveNumberProp"] = classToRedact.TransitiveNumberProp.ToString(CultureInfo.InvariantCulture),
            ["complexParam_TransitiveStringProp"] = new('*', classToRedact.TransitiveStringProp.Length),
        };

        _logger.Collector.LatestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }
}
