// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using TestClasses;
using Xunit;

using static TestClasses.LogPropertiesRedactionExtensions;

namespace Microsoft.Gen.Logging.Test;

public class LogPropertiesRedactionTests
{
    [Fact]
    public void RedactsWhenRedactorProviderIsAvailableInTheInstance()
    {
        using var logger = Utils.GetLogger();
        var collector = logger.FakeLogCollector;

        var instance = new NonStaticTestClass(logger);
        var classToRedact = new MyBaseClassToRedact();

        instance.LogPropertiesWithRedaction("arg0", classToRedact);

        Assert.Equal(1, collector.Count);
        Assert.Null(collector.LatestRecord.Exception);
        Assert.Equal("LogProperties with redaction: ----", collector.LatestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["P0"] = "----",
            ["p1.StringPropertyBase"] = new('-', classToRedact.StringPropertyBase.Length),
            ["{OriginalFormat}"] = "LogProperties with redaction: {P0}"
        };

        collector.LatestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void RedactsWhenDefaultAttrCtorAndRedactorProviderIsInTheInstance()
    {
        using var logger = Utils.GetLogger();
        var collector = logger.FakeLogCollector;

        var instance = new NonStaticTestClass(logger);
        var classToRedact = new MyBaseClassToRedact();

        instance.DefaultAttrCtorLogPropertiesWithRedaction(LogLevel.Information, "arg0", classToRedact);

        Assert.Equal(1, collector.Count);
        Assert.Null(collector.LatestRecord.Exception);
        Assert.Equal(LogLevel.Information, collector.LatestRecord.Level);
        Assert.Equal(string.Empty, collector.LatestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["p0"] = "----",
            ["p1.StringPropertyBase"] = new('-', classToRedact.StringPropertyBase.Length),
        };

        collector.LatestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void RedactsWhenLogMethodIsStaticNoParams()
    {
        using var logger = Utils.GetLogger();
        var collector = logger.FakeLogCollector;

        var classToRedact = new ClassToRedact();

        LogNoParams(logger, classToRedact);

        Assert.Equal(1, collector.Count);
        Assert.Null(collector.LatestRecord.Exception);
        Assert.Equal("No template params", collector.LatestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["classToLog.StringProperty"] = new('+', classToRedact.StringProperty.Length),
            ["classToLog.StringPropertyBase"] = new('-', classToRedact.StringPropertyBase.Length),
            ["classToLog.SimplifiedNullableIntProperty"] = classToRedact.SimplifiedNullableIntProperty.ToString(CultureInfo.InvariantCulture),
            ["classToLog.GetOnlyProperty"] = new('-', classToRedact.GetOnlyProperty.Length),
            ["classToLog.TransitiveProp.TransitiveNumberProp"] = classToRedact.TransitiveProp.TransitiveNumberProp.ToString(CultureInfo.InvariantCulture),
            ["classToLog.TransitiveProp.TransitiveStringProp"] = new('-', classToRedact.TransitiveProp.TransitiveStringProp.Length),
            ["classToLog.NoRedactionProp"] = classToRedact.NoRedactionProp,
            ["classToLog.NonFormattableProperty"] = new('-', classToRedact.NonFormattableProperty.ToString().Length),
            ["{OriginalFormat}"] = "No template params"
        };

        collector.LatestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void RedactsWhenDefaultAttrCtorAndIsStaticNoParams()
    {
        using var logger = Utils.GetLogger();
        var collector = logger.FakeLogCollector;

        var classToRedact = new ClassToRedact();

        LogNoParamsDefaultCtor(logger, LogLevel.Warning, classToRedact);

        Assert.Equal(1, collector.Count);
        Assert.Null(collector.LatestRecord.Exception);
        Assert.Equal(LogLevel.Warning, collector.LatestRecord.Level);
        Assert.Equal(string.Empty, collector.LatestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["classToLog.StringProperty"] = new('+', classToRedact.StringProperty.Length),
            ["classToLog.StringPropertyBase"] = new('-', classToRedact.StringPropertyBase.Length),
            ["classToLog.SimplifiedNullableIntProperty"] = classToRedact.SimplifiedNullableIntProperty.ToString(CultureInfo.InvariantCulture),
            ["classToLog.GetOnlyProperty"] = new('-', classToRedact.GetOnlyProperty.Length),
            ["classToLog.TransitiveProp.TransitiveNumberProp"] = classToRedact.TransitiveProp.TransitiveNumberProp.ToString(CultureInfo.InvariantCulture),
            ["classToLog.TransitiveProp.TransitiveStringProp"] = new('-', classToRedact.TransitiveProp.TransitiveStringProp.Length),
            ["classToLog.NoRedactionProp"] = classToRedact.NoRedactionProp,
            ["classToLog.NonFormattableProperty"] = new('-', classToRedact.NonFormattableProperty.ToString().Length),
        };

        collector.LatestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void RedactsWhenLogMethodIsStaticTwoParams()
    {
        using var logger = Utils.GetLogger();
        var collector = logger.FakeLogCollector;

        var classToRedact = new MyTransitiveClass();

        LogTwoParams(logger, "string_prop", classToRedact);

        Assert.Equal(1, collector.Count);
        Assert.Null(collector.LatestRecord.Exception);
        Assert.Equal("Only ----------- as param", collector.LatestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["StringProperty"] = "-----------",
            ["complexParam.TransitiveNumberProp"] = classToRedact.TransitiveNumberProp.ToString(CultureInfo.InvariantCulture),
            ["complexParam.TransitiveStringProp"] = new('-', classToRedact.TransitiveStringProp.Length),
            ["{OriginalFormat}"] = "Only {StringProperty} as param"
        };

        collector.LatestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void RedactsWhenDefaultAttrCtorAndIsStaticTwoParams()
    {
        using var logger = Utils.GetLogger();
        var collector = logger.FakeLogCollector;

        var classToRedact = new MyTransitiveClass();

        LogTwoParamsDefaultCtor(logger, LogLevel.None, "string_prop", classToRedact);

        Assert.Equal(1, collector.Count);
        Assert.Null(collector.LatestRecord.Exception);
        Assert.Equal(LogLevel.None, collector.LatestRecord.Level);
        Assert.Equal(string.Empty, collector.LatestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["stringProperty"] = "-----------",
            ["complexParam.TransitiveNumberProp"] = classToRedact.TransitiveNumberProp.ToString(CultureInfo.InvariantCulture),
            ["complexParam.TransitiveStringProp"] = new('-', classToRedact.TransitiveStringProp.Length),
        };

        collector.LatestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void RedactsWhenSensitiveRecordIsInMessageTemplate()
    {
        using var logger = Utils.GetLogger();
        var collector = logger.FakeLogCollector;

        InlineRecord record = new("Secret data...");
        LogSensitiveRecord(logger, record);

        var logRecord = Assert.Single(collector.GetSnapshot());
        Assert.Null(logRecord.Exception);
        Assert.Equal(LogLevel.Error, logRecord.Level);

        // FIXME: fails here
        Assert.DoesNotContain("secret", logRecord.Message, System.StringComparison.OrdinalIgnoreCase);

        Assert.NotNull(logRecord.StructuredState);
        var prop = Assert.Single(logRecord.StructuredState, x => x.Key == "rec_InlineProp");

        // FIXME: fails here too
        Assert.DoesNotContain("secret", prop.Value, System.StringComparison.OrdinalIgnoreCase);
    }
}
