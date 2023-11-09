// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Shared.Text;
using TestClasses;
using Xunit;

namespace Microsoft.Gen.Logging.Test;

public class TagProviderTests
{
    private readonly FakeLogger _logger = new();

    internal class ClassToBeLogged
    {
        public string MyStringProperty { get; set; } = "TestString";
        public override string ToString() => "ClassToLog string representation";
    }

    [Fact]
    public void LogsWithObject()
    {
        object obj = new ClassToBeLogged();
        TagProviderWithObjectExtensions.OneParam(_logger, obj);

        Assert.Equal(1, _logger.Collector.Count);

        var latestRecord = _logger.Collector.LatestRecord;
        Assert.Equal(LogLevel.Warning, latestRecord.Level);
        Assert.Equal($"Custom provided properties for {obj}.", latestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["Param"] = obj.ToString(),
            ["param.ToString"] = obj + " ProvidePropertiesCall",
            ["{OriginalFormat}"] = "Custom provided properties for {Param}."
        };

        latestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void LogsWhenNonStaticClass()
    {
        const string StringParamValue = "Value for a string";

        var classToLog = new ClassToLog { MyIntProperty = 0 };
        new NonStaticTestClass(_logger).LogPropertiesWithProvider(StringParamValue, classToLog);

        Assert.Equal(1, _logger.Collector.Count);
        var latestRecord = _logger.Collector.LatestRecord;
        Assert.Equal(LogLevel.Information, latestRecord.Level);
        Assert.Equal($"LogProperties with provider: {StringParamValue}, {classToLog}", latestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["P0"] = StringParamValue,
            ["P1"] = classToLog.ToString(),
            ["p1.MyIntProperty"] = classToLog.MyIntProperty.ToInvariantString(),
            ["p1.Custom_property_name"] = classToLog.MyStringProperty,
            ["{OriginalFormat}"] = "LogProperties with provider: {P0}, {P1}"
        };

        latestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void LogsWhenDefaultAttrCtorInNonStaticClass()
    {
        const string StringParamValue = "Value for a string";

        var classToLog = new ClassToLog { MyIntProperty = ushort.MaxValue };
        new NonStaticTestClass(_logger).DefaultAttrCtorLogPropertiesWithProvider(LogLevel.Debug, StringParamValue, classToLog);

        Assert.Equal(1, _logger.Collector.Count);
        var latestRecord = _logger.Collector.LatestRecord;
        Assert.Null(latestRecord.Exception);
        Assert.Equal(0, latestRecord.Id.Id);
        Assert.Equal(LogLevel.Debug, latestRecord.Level);
        Assert.Equal(string.Empty, latestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["p0"] = StringParamValue,
            ["p1.MyIntProperty"] = classToLog.MyIntProperty.ToInvariantString(),
            ["p1.Custom_property_name"] = classToLog.MyStringProperty
        };

        latestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void LogsWhenDefaultAttrCtorInStaticClass()
    {
        var classToLog = new ClassToLog { MyIntProperty = ushort.MaxValue };
        TagProviderExtensions.DefaultAttributeCtor(_logger, LogLevel.Trace, classToLog);

        Assert.Equal(1, _logger.Collector.Count);
        var latestRecord = _logger.Collector.LatestRecord;
        Assert.Null(latestRecord.Exception);
        Assert.Equal(0, latestRecord.Id.Id);
        Assert.Equal(LogLevel.Trace, latestRecord.Level);
        Assert.Equal(string.Empty, latestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["param.MyIntProperty"] = classToLog.MyIntProperty.ToInvariantString(),
            ["param.Custom_property_name"] = classToLog.MyStringProperty
        };

        latestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void LogsWithNullable()
    {
        TagProviderExtensions.Nullable(_logger, LogLevel.Trace, null);

        Assert.Equal(1, _logger.Collector.Count);
        var latestRecord = _logger.Collector.LatestRecord;
        Assert.Null(latestRecord.Exception);
        Assert.Equal(0, latestRecord.Id.Id);
        Assert.Equal(LogLevel.Trace, latestRecord.Level);
        Assert.Equal(string.Empty, latestRecord.Message);
        Assert.Empty(latestRecord.StructuredState!);

        _logger.Collector.Clear();
        TagProviderExtensions.Nullable(_logger, LogLevel.Trace, 42);

        Assert.Equal(1, _logger.Collector.Count);
        latestRecord = _logger.Collector.LatestRecord;
        Assert.Null(latestRecord.Exception);
        Assert.Equal(0, latestRecord.Id.Id);
        Assert.Equal(LogLevel.Trace, latestRecord.Level);
        Assert.Equal(string.Empty, latestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["param.P1"] = "42",
        };

        latestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void LogsWhenOmitParamNameIsTrue()
    {
        var props = new LogPropertiesOmitParameterNameExtensions.MyProps
        {
            P0 = 42,
            P1 = "foo"
        };

        LogPropertiesOmitParameterNameExtensions.M1(_logger, props);

        Assert.Equal(1, _logger.Collector.Count);
        var latestRecord = _logger.Collector.LatestRecord;
        Assert.Equal(LogLevel.Warning, latestRecord.Level);
        Assert.Equal(string.Empty, latestRecord.Message);

        var state = latestRecord.StructuredState!;
        Assert.Equal(2, state.Count);
        Assert.Equal("P0", state[0].Key);
        Assert.Equal(props.P0.ToString(CultureInfo.InvariantCulture), state[0].Value);
        Assert.Equal("Custom_property_name", state[1].Key);
        Assert.Equal(props.P1, state[1].Value);
    }

    [Fact]
    public void LogsWhenOmitParamNameIsTrueWithDefaultAttrCtor()
    {
        var props = new LogPropertiesOmitParameterNameExtensions.MyProps
        {
            P0 = 42,
            P1 = "foo"
        };

        LogPropertiesOmitParameterNameExtensions.M3(_logger, LogLevel.Error, props);

        Assert.Equal(1, _logger.Collector.Count);
        var latestRecord = _logger.Collector.LatestRecord;
        Assert.Null(latestRecord.Exception);
        Assert.Equal(0, latestRecord.Id.Id);
        Assert.Equal(LogLevel.Error, latestRecord.Level);
        Assert.Equal(string.Empty, latestRecord.Message);

        var state = latestRecord.StructuredState!;
        Assert.Equal(2, state.Count);
        Assert.Equal("P0", state[0].Key);
        Assert.Equal(props.P0.ToString(CultureInfo.InvariantCulture), state[0].Value);
        Assert.Equal("Custom_property_name", state[1].Key);
        Assert.Equal(props.P1, state[1].Value);
    }

    [Fact]
    public void LogsWithNullObject()
    {
        TagProviderWithObjectExtensions.OneParam(_logger, null!);

        Assert.Equal(1, _logger.Collector.Count);

        var latestRecord = _logger.Collector.LatestRecord;
        Assert.Equal(LogLevel.Warning, latestRecord.Level);
        Assert.Equal("Custom provided properties for (null).", latestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["Param"] = null,
            ["{OriginalFormat}"] = "Custom provided properties for {Param}."
        };

        latestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void LogsWhenNullStronglyTypedObject()
    {
        TagProviderExtensions.LogMethodCustomPropsProvider(_logger, null!);

        Assert.Equal(1, _logger.Collector.Count);
        var latestRecord = _logger.Collector.LatestRecord;
        Assert.Equal(LogLevel.Warning, latestRecord.Level);
        Assert.Equal("Custom provided properties for (null).", latestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["Param"] = null,
            ["{OriginalFormat}"] = "Custom provided properties for {Param}."
        };

        latestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void LogsWhenNonNullStronglyTypedObject()
    {
        var classToLog = new ClassToLog { MyIntProperty = 0 };
        TagProviderExtensions.LogMethodCustomPropsProvider(_logger, classToLog);

        Assert.Equal(1, _logger.Collector.Count);
        var latestRecord = _logger.Collector.LatestRecord;
        Assert.Equal(LogLevel.Warning, latestRecord.Level);
        Assert.Equal($"Custom provided properties for {classToLog}.", latestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["Param"] = classToLog.ToString(),
            ["param.MyIntProperty"] = classToLog.MyIntProperty.ToInvariantString(),
            ["param.Custom_property_name"] = classToLog.MyStringProperty,
            ["{OriginalFormat}"] = "Custom provided properties for {Param}."
        };

        latestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void LogsWhenStruct()
    {
        var structToLog = new StructToLog { MyIntProperty = 0 };
        TagProviderExtensions.LogMethodCustomPropsProviderStruct(_logger, structToLog);

        Assert.Equal(1, _logger.Collector.Count);
        var latestRecord = _logger.Collector.LatestRecord;
        Assert.Equal(LogLevel.Debug, latestRecord.Level);
        Assert.Equal($"Custom provided properties for struct.", latestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["param.MyIntProperty"] = structToLog.MyIntProperty.ToInvariantString(),
            ["param.Custom_property_name"] = structToLog.MyStringProperty,
            ["{OriginalFormat}"] = "Custom provided properties for struct."
        };

        latestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void LogsWhenInterface()
    {
        IInterfaceToLog interfaceToLog = new InterfaceImpl { MyIntProperty = 0 };
        TagProviderExtensions.LogMethodCustomPropsProviderInterface(_logger, interfaceToLog);

        Assert.Equal(1, _logger.Collector.Count);
        var latestRecord = _logger.Collector.LatestRecord;
        Assert.Equal(LogLevel.Information, latestRecord.Level);
        Assert.Equal($"Custom provided properties for interface.", latestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["param.MyIntProperty"] = interfaceToLog.MyIntProperty.ToInvariantString(),
            ["param.Custom_property_name"] = interfaceToLog.MyStringProperty,
            ["{OriginalFormat}"] = "Custom provided properties for interface."
        };

        latestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void LogsWhenProviderCombinedWithLogProperties()
    {
        var classToLog = new ClassToLog { MyIntProperty = 0 };
        TagProviderExtensions.LogMethodCombinePropsProvider(_logger, classToLog, classToLog);

        Assert.Equal(1, _logger.Collector.Count);
        var latestRecord = _logger.Collector.LatestRecord;
        Assert.Equal(LogLevel.Warning, latestRecord.Level);
        Assert.Equal("No params.", latestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["param1.MyIntProperty"] = classToLog.MyIntProperty.ToInvariantString(),
            ["param1.MyStringProperty"] = classToLog.MyStringProperty,
            ["param1.AnotherStringProperty"] = classToLog.AnotherStringProperty,
            ["param2.MyIntProperty"] = classToLog.MyIntProperty.ToInvariantString(),
            ["param2.Custom_property_name"] = classToLog.MyStringProperty,
            ["{OriginalFormat}"] = "No params."
        };

        latestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void LogsTwoStronglyTypedParams()
    {
        const string StringParamValue = "Value for a string";

        var classToLog1 = new ClassToLog { MyIntProperty = 1 };
        var classToLog2 = new ClassToLog { MyIntProperty = -1 };
        TagProviderExtensions.LogMethodCustomPropsProviderTwoParams(_logger, StringParamValue, classToLog1, classToLog2);

        Assert.Equal(1, _logger.Collector.Count);
        var latestRecord = _logger.Collector.LatestRecord;
        Assert.Equal(LogLevel.Warning, latestRecord.Level);
        Assert.Equal($"Custom provided properties for both complex params and {StringParamValue}.", latestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["StringParam"] = StringParamValue,
            ["param.MyIntProperty"] = classToLog1.MyIntProperty.ToInvariantString(),
            ["param.Custom_property_name"] = classToLog1.MyStringProperty,
            ["param2.Another_property_name"] = classToLog2.MyStringProperty.ToUpperInvariant(),
            ["param2.MyIntProperty_test"] = classToLog2.MyIntProperty.ToInvariantString(),
            ["{OriginalFormat}"] = "Custom provided properties for both complex params and {StringParam}."
        };

        latestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);

        // Changing object and logging again to test that IResettable for props provider works correctly:
        classToLog1.MyIntProperty = int.MaxValue;
        classToLog2.MyIntProperty = int.MinValue;
        TagProviderExtensions.LogMethodCustomPropsProviderTwoParams(_logger, StringParamValue, classToLog1, classToLog2);

        Assert.Equal(2, _logger.Collector.Count);
        expectedState["param.MyIntProperty"] = classToLog1.MyIntProperty.ToInvariantString();
        expectedState["param2.MyIntProperty_test"] = classToLog2.MyIntProperty.ToInvariantString();
        _logger.Collector.LatestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void LogsTwoObjectParams()
    {
        const string StringParamValue = "ValueForAString";

        object obj1 = new ClassToBeLogged();
        object obj2 = new ClassToBeLogged();
        TagProviderWithObjectExtensions.TwoParams(_logger, StringParamValue, obj1, obj2);

        Assert.Equal(1, _logger.Collector.Count);

        var latestRecord = _logger.Collector.LatestRecord;
        Assert.Equal(LogLevel.Warning, latestRecord.Level);
        Assert.Equal($"Custom provided properties for both complex params and {StringParamValue}.", latestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["StringParam"] = StringParamValue,
            ["param.ToString"] = obj1 + " ProvidePropertiesCall",
            ["param2.Type"] = obj2.GetType().ToString(),
            ["param2.ToString"] = obj2 + " ProvideOtherPropertiesCall",
            ["{OriginalFormat}"] = "Custom provided properties for both complex params and {StringParam}."
        };

        latestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void LogsTwoNullObjectParams()
    {
        const string StringParamValue = "ValueForAString";

        TagProviderWithObjectExtensions.TwoParams(_logger, StringParamValue, null!, null!);

        Assert.Equal(1, _logger.Collector.Count);

        var latestRecord = _logger.Collector.LatestRecord;
        Assert.Equal(LogLevel.Warning, latestRecord.Level);
        Assert.Equal($"Custom provided properties for both complex params and {StringParamValue}.", latestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["StringParam"] = StringParamValue,
            ["param2.Type"] = null,
            ["param2.ToString"] = " ProvideOtherPropertiesCall",
            ["{OriginalFormat}"] = "Custom provided properties for both complex params and {StringParam}."
        };

        latestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }
}
