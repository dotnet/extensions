// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Numerics;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Shared.Text;
using TestClasses;
using Xunit;
using static TestClasses.LogPropertiesExtensions;
using static TestClasses.LogPropertiesRecordExtensions;

namespace Microsoft.Gen.Logging.Test;

public class LogPropertiesTests
{
    private const string StringProperty = "microsoft.com";
    private readonly FakeLogger _logger = new();

    [Fact]
    public void LogPropertiesEnumerables()
    {
        var props = new LogPropertiesSimpleExtensions.MyProps
        {
            P5 = new[] { 1, 2, 3 },
            P6 = new[] { 4, 5, 6 },
            P7 = new Dictionary<string, int>
            {
                { "Seven", 7 },
                { "Eight", 8 },
                { "Nine", 9 }
            }
        };

        LogPropertiesSimpleExtensions.LogFunc(_logger, "Hello", props);

        Assert.Equal(1, _logger.Collector.Count);

        var ss = _logger.LatestRecord.StructuredState!.ToDictionary(x => x.Key, x => x.Value);
        Assert.Equal("[\"1\",\"2\",\"3\"]", ss["myProps.P5"]);
        Assert.Equal("[\"4\",\"5\",\"6\"]", ss["myProps.P6"]);
        Assert.Equal("{\"Seven\"=\"7\",\"Eight\"=\"8\",\"Nine\"=\"9\"}", ss["myProps.P7"]);
    }

    [Fact]
    public void LogPropertiesOmitParamName()
    {
        var props = new LogPropertiesOmitParameterNameExtensions.MyProps
        {
            P0 = 42,
            P1 = "foo"
        };

        LogPropertiesOmitParameterNameExtensions.M0(_logger, props);

        var state = _logger.LatestRecord.StructuredState!;
        Assert.Equal(2, state.Count);
        Assert.Equal(props.P0.ToString(CultureInfo.InvariantCulture), state!.GetValue("P0"));
        Assert.Equal(props.P1, state!.GetValue("P1"));
    }

    [Fact]
    public void LogPropertiesOmitParamNameDefaultAttrCtor()
    {
        var props = new LogPropertiesOmitParameterNameExtensions.MyProps
        {
            P0 = 42,
            P1 = "foo"
        };

        LogPropertiesOmitParameterNameExtensions.M2(_logger, LogLevel.Critical, props);

        var state = _logger.LatestRecord.StructuredState!;
        Assert.Equal(2, state.Count);
        Assert.Equal(props.P0.ToString(CultureInfo.InvariantCulture), state!.GetValue("P0"));
        Assert.Equal(props.P1, state!.GetValue("P1"));
    }

    [Fact]
    public void LogPropertiesSpecialTypes()
    {
        var props = new LogPropertiesSpecialTypesExtensions.MyProps
        {
            P0 = DateTime.Now,
            P1 = DateTimeOffset.Now,
            P2 = new TimeSpan(1234),
            P3 = Guid.NewGuid(),
            P4 = new Version(1, 2, 3, 4),
            P5 = new Uri("https://www.microsoft.com"),
            P6 = IPAddress.Parse("192.168.10.1"),
            P7 = new IPEndPoint(IPAddress.Parse("192.168.10.1"), 42),
            P8 = new IPEndPoint(IPAddress.Parse("192.168.10.1"), 42),
            P9 = new DnsEndPoint("microsoft.com", 42),
            P10 = new BigInteger(3.1415),
            P11 = new Complex(1.2, 3.4),
            P12 = new Matrix3x2(1, 2, 3, 4, 5, 6),
            P13 = new Matrix4x4(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16),
            P14 = new Plane(1, 2, 3, 4),
            P15 = new Quaternion(1, 2, 3, 4),
            P16 = new Vector2(1),
            P17 = new Vector3(1, 2, 3),
            P18 = new Vector4(1, 2, 3, 4),

#if NET6_0_OR_GREATER
            P19 = new TimeOnly(123),
            P20 = new DateOnly(2022, 6, 21),
#endif
        };

        LogPropertiesSpecialTypesExtensions.M0(_logger, props);
        var state = _logger.LatestRecord.StructuredState!;

#if NET6_0_OR_GREATER
        Assert.Equal(21, state.Count);
#else
        Assert.Equal(19, state.Count);
#endif

        Assert.Equal(props.P0.ToString(CultureInfo.InvariantCulture), state!.GetValue("p.P0"));
        Assert.Equal(props.P1.ToString(CultureInfo.InvariantCulture), state!.GetValue("p.P1"));
        Assert.Equal(props.P2.ToString(null, CultureInfo.InvariantCulture), state!.GetValue("p.P2"));
        Assert.Equal(props.P3.ToString(), state!.GetValue("p.P3"));
        Assert.Equal(props.P4.ToString(), state!.GetValue("p.P4"));
        Assert.Equal(props.P5.ToString(), state!.GetValue("p.P5"));
        Assert.Equal(props.P6.ToString(), state!.GetValue("p.P6"));
        Assert.Equal(props.P7.ToString(), state!.GetValue("p.P7"));
        Assert.Equal(props.P8.ToString(), state!.GetValue("p.P8"));
        Assert.Equal(props.P9.ToString(), state!.GetValue("p.P9"));
        Assert.Equal(props.P10.ToString(CultureInfo.InvariantCulture), state!.GetValue("p.P10"));
        Assert.Equal(props.P11.ToString(CultureInfo.InvariantCulture), state!.GetValue("p.P11"));
        Assert.Equal(props.P12.ToString(), state!.GetValue("p.P12"));
        Assert.Equal(props.P13.ToString(), state!.GetValue("p.P13"));
        Assert.Equal(props.P14.ToString(), state!.GetValue("p.P14"));
        Assert.Equal(props.P15.ToString(), state!.GetValue("p.P15"));
        Assert.Equal(props.P16.ToString(), state!.GetValue("p.P16"));
        Assert.Equal(props.P17.ToString(), state!.GetValue("p.P17"));
        Assert.Equal(props.P18.ToString(), state!.GetValue("p.P18"));

#if NET6_0_OR_GREATER
        Assert.Equal(props.P19.ToString(CultureInfo.InvariantCulture), state!.GetValue("p.P19"));
        Assert.Equal(props.P20.ToString(CultureInfo.InvariantCulture), state!.GetValue("p.P20"));
#endif

    }

    [Fact]
    public void LogPropertiesNullHandling()
    {
        using var logger = Utils.GetLogger();
        var collector = logger.FakeLogCollector;

        var props = new LogPropertiesNullHandlingExtensions.MyProps
        {
            P0 = null!,
            P1 = null,
            P2 = 2,
            P3 = null,
            P4 = default,
            P5 = null!,
            P6 = null,
            P7 = 2,
            P8 = null,
            P9 = default,
            P10 = default,
        };

        LogPropertiesNullHandlingExtensions.M0(logger, props);
        Assert.Equal(1, collector.Count);

        var ss = collector.LatestRecord.StructuredState!.ToDictionary(x => x.Key, x => x.Value);
        Assert.Equal(11, ss.Count);
        Assert.Null(ss["p.P0"]);
        Assert.Null(ss["p.P1"]);
        Assert.Equal(props.P2.ToString(null, CultureInfo.InvariantCulture), ss["p.P2"]);
        Assert.Null(ss["p.P3"]);
        Assert.Equal("I refuse to be formatted", ss["p.P4"]);
        Assert.Null(ss["p.P5"]);
        Assert.Null(ss["p.P6"]);
        Assert.Equal("-", ss["p.P7"]);
        Assert.Null(ss["p.P8"]);
        Assert.Equal("------------------------", ss["p.P9"]);
        Assert.Equal("null", ss["p.P10"]);

        collector.Clear();
        LogPropertiesNullHandlingExtensions.M1(logger, props);
        Assert.Equal(1, collector.Count);

        ss = collector.LatestRecord.StructuredState!.ToDictionary(x => x.Key, x => x.Value);
        Assert.Equal(4, ss.Count);
        Assert.Equal(props.P2.ToString(null, CultureInfo.InvariantCulture), ss["p.P2"]);
        Assert.Equal("I refuse to be formatted", ss["p.P4"]);
        Assert.Equal("-", ss["p.P7"]);
        Assert.Equal("------------------------", ss["p.P9"]);
    }

    [Fact]
    public void LogPropertiesTest()
    {
        var classToLog = new MyDerivedClass(double.Epsilon)
        {
            StringProperty = "test Abc",
            SimplifiedNullableIntProperty = null,
            ExplicitNullableIntProperty = 2,
            StringPropertyBase = "Base Abc",
            SetOnlyProperty = DateTime.MinValue,
            NonVirtualPropertyBase = "NonVirtualPropertyBase",
            TransitivePropertyArray = new[] { 1, 2, 3 },
            TransitiveProperty = new MyTransitiveDerivedClass
            {
                TransitiveStringProp = "Transitive string",
                TransitiveVirtualProp = int.MaxValue,
                InnerTransitiveProperty = new LeafTransitiveDerivedClass(),
                TransitiveDerivedProp = byte.MaxValue
            },
            AnotherTransitiveProperty = null,
            VirtualInterimProperty = -1,
            PropertyOfGenerics = new GenericClass<int> { GenericProp = 1 },
            InterimProperty = short.MaxValue
        };

        LogFunc(_logger, StringProperty, classToLog);
        Assert.Equal(1, _logger.Collector.Count);
        Assert.Equal(LogLevel.Debug, _logger.Collector.LatestRecord.Level);

        var expectedState = new Dictionary<string, string?>
        {
            ["classToLog_StringProperty_1"] = "microsoft.com",
            ["classToLog.StringProperty"] = classToLog.StringProperty,
            ["classToLog.SimplifiedNullableIntProperty"] = null,
            ["classToLog.ExplicitNullableIntProperty"] = classToLog.ExplicitNullableIntProperty.ToString(),
            ["classToLog.GetOnlyProperty"] = classToLog.GetOnlyProperty.ToString(CultureInfo.InvariantCulture),
            ["classToLog.VirtualPropertyBase"] = classToLog.VirtualPropertyBase,
            ["classToLog.NonVirtualPropertyBase"] = classToLog.NonVirtualPropertyBase,
            ["classToLog.TransitivePropertyArray"] = LoggerMessageHelper.Stringify(classToLog.TransitivePropertyArray),
            ["classToLog.TransitiveProperty.TransitiveNumberProp"]
                = classToLog.TransitiveProperty.TransitiveNumberProp.ToString(CultureInfo.InvariantCulture),

            ["classToLog.TransitiveProperty.TransitiveStringProp"] = classToLog.TransitiveProperty.TransitiveStringProp,
            ["classToLog.TransitiveProperty.InnerTransitiveProperty.IntegerProperty"]
                = classToLog.TransitiveProperty.InnerTransitiveProperty.IntegerProperty.ToString(CultureInfo.InvariantCulture),
            ["classToLog.TransitiveProperty.InnerTransitiveProperty.DateTimeProperty"]
                = classToLog.TransitiveProperty.InnerTransitiveProperty.DateTimeProperty.ToString(CultureInfo.InvariantCulture),

            ["classToLog.AnotherTransitiveProperty.IntegerProperty"] = null, // Since AnotherTransitiveProperty is null
            ["classToLog.StringPropertyBase"] = classToLog.StringPropertyBase,
            ["classToLog.VirtualInterimProperty"] = classToLog.VirtualInterimProperty.ToInvariantString(),
            ["classToLog.InterimProperty"] = classToLog.InterimProperty.ToString(CultureInfo.InvariantCulture),
            ["classToLog.TransitiveProperty.TransitiveDerivedProp"] = classToLog.TransitiveProperty.TransitiveDerivedProp.ToInvariantString(),
            ["classToLog.TransitiveProperty.TransitiveVirtualProp"] = classToLog.TransitiveProperty.TransitiveVirtualProp.ToInvariantString(),
            ["classToLog.TransitiveProperty.TransitiveGenericProp.GenericProp"]
                = classToLog.TransitiveProperty.TransitiveGenericProp.GenericProp,

            ["classToLog.PropertyOfGenerics.GenericProp"] = classToLog.PropertyOfGenerics.GenericProp.ToInvariantString(),
            ["classToLog.CustomStructProperty.LongProperty"] = classToLog.CustomStructProperty.LongProperty.ToInvariantString(),
            ["classToLog.CustomStructProperty.TransitiveStructProperty.DateTimeOffsetProperty"]
                = classToLog.CustomStructProperty.TransitiveStructProperty.DateTimeOffsetProperty.ToString(CultureInfo.InvariantCulture),

            ["classToLog.CustomStructProperty.NullableTransitiveStructProperty.DateTimeOffsetProperty"]
                = classToLog.CustomStructProperty.NullableTransitiveStructProperty?.DateTimeOffsetProperty.ToString(CultureInfo.InvariantCulture),

            ["classToLog.CustomStructProperty.NullableTransitiveStructProperty2.DateTimeOffsetProperty"]
                = classToLog.CustomStructProperty.NullableTransitiveStructProperty2?.DateTimeOffsetProperty.ToString(CultureInfo.InvariantCulture),

            ["classToLog.CustomStructNullableProperty.LongProperty"] = classToLog.CustomStructNullableProperty?.LongProperty.ToInvariantString(),
            ["classToLog.CustomStructNullableProperty.TransitiveStructProperty.DateTimeOffsetProperty"]
                = classToLog.CustomStructNullableProperty?.TransitiveStructProperty.DateTimeOffsetProperty.ToString(CultureInfo.InvariantCulture),

            ["classToLog.CustomStructNullableProperty.NullableTransitiveStructProperty.DateTimeOffsetProperty"]
                = classToLog.CustomStructNullableProperty?.NullableTransitiveStructProperty?.DateTimeOffsetProperty.ToString(CultureInfo.InvariantCulture),

            ["classToLog.CustomStructNullableProperty.NullableTransitiveStructProperty2.DateTimeOffsetProperty"]
                = classToLog.CustomStructNullableProperty?.NullableTransitiveStructProperty2?.DateTimeOffsetProperty.ToString(CultureInfo.InvariantCulture),

            ["classToLog.CustomStructNullableProperty2.LongProperty"] = classToLog.CustomStructNullableProperty2?.LongProperty.ToInvariantString(),
            ["classToLog.CustomStructNullableProperty2.TransitiveStructProperty.DateTimeOffsetProperty"]
                = classToLog.CustomStructNullableProperty2?.TransitiveStructProperty.DateTimeOffsetProperty.ToString(CultureInfo.InvariantCulture),

            ["classToLog.CustomStructNullableProperty2.NullableTransitiveStructProperty.DateTimeOffsetProperty"]
                = classToLog.CustomStructNullableProperty2?.NullableTransitiveStructProperty?.DateTimeOffsetProperty.ToString(CultureInfo.InvariantCulture),

            ["classToLog.CustomStructNullableProperty2.NullableTransitiveStructProperty2.DateTimeOffsetProperty"]
                = classToLog.CustomStructNullableProperty2?.NullableTransitiveStructProperty2?.DateTimeOffsetProperty.ToString(CultureInfo.InvariantCulture),

            ["{OriginalFormat}"] = "Only {classToLog_StringProperty_1} as param"
        };

        _logger.Collector.LatestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void LogPropertiesInTemplateTest()
    {
        var classToLog = new ClassAsParam { MyProperty = 0 };
        LogMethodTwoParams(_logger, StringProperty, classToLog);
        Assert.Equal(1, _logger.Collector.Count);
        Assert.Equal(LogLevel.Information, _logger.Collector.LatestRecord.Level);
        Assert.Equal($"Both {StringProperty} and {classToLog} as params", _logger.Collector.LatestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["StringProperty"] = StringProperty,
            ["ComplexParam"] = classToLog.ToString(),
            ["complexParam.MyProperty"] = classToLog.MyProperty.ToInvariantString(),
            ["{OriginalFormat}"] = "Both {StringProperty} and {ComplexParam} as params"
        };

        _logger.Collector.LatestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void LogPropertiesNonStaticClassTest()
    {
        const string StringParamValue = "Value for a string";

        var classToLog = new ClassToLog { MyIntProperty = 0 };
        new NonStaticTestClass(_logger).LogProperties(StringParamValue, classToLog);

        Assert.Equal(1, _logger.Collector.Count);
        var latestRecord = _logger.Collector.LatestRecord;
        Assert.Equal(LogLevel.Information, latestRecord.Level);
        Assert.Equal($"LogProperties: {StringParamValue}", latestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["P0"] = StringParamValue,
            ["p1.MyIntProperty"] = classToLog.MyIntProperty.ToInvariantString(),
            ["p1.MyStringProperty"] = classToLog.MyStringProperty,
            ["p1.AnotherStringProperty"] = classToLog.AnotherStringProperty,
            ["{OriginalFormat}"] = "LogProperties: {P0}"
        };

        latestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void LogPropertyTestCustomStruct()
    {
        var transitiveStruct = new MyTransitiveStruct { DateTimeOffsetProperty = DateTimeOffset.MinValue };
        var structToLog = new MyCustomStruct { LongProperty = 1L, NullableTransitiveStructProperty2 = transitiveStruct };
        LogMethodStruct(_logger, structToLog);

        Assert.Equal(1, _logger.Collector.Count);
        var latestRecord = _logger.Collector.LatestRecord;
        Assert.Null(latestRecord.Exception);
        Assert.Equal("Testing non-nullable struct here...", latestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["structParam.LongProperty"] = structToLog.LongProperty.ToInvariantString(),
            ["structParam.TransitiveStructProperty.DateTimeOffsetProperty"]
                = structToLog.TransitiveStructProperty.DateTimeOffsetProperty.ToString(CultureInfo.InvariantCulture),

            ["structParam.NullableTransitiveStructProperty.DateTimeOffsetProperty"]
                = structToLog.NullableTransitiveStructProperty?.DateTimeOffsetProperty.ToString(CultureInfo.InvariantCulture),

            ["structParam.NullableTransitiveStructProperty2.DateTimeOffsetProperty"]
                = structToLog.NullableTransitiveStructProperty2.Value.DateTimeOffsetProperty.ToString(CultureInfo.InvariantCulture),

            ["{OriginalFormat}"] = "Testing non-nullable struct here..."
        };

        latestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void LogPropertyTestCustomNullableStruct()
    {
        var transitiveStruct = new MyTransitiveStruct { DateTimeOffsetProperty = DateTimeOffset.MinValue };
        MyCustomStruct? structToLog = new MyCustomStruct { LongProperty = 1L, NullableTransitiveStructProperty = transitiveStruct };
        LogMethodNullableStruct(_logger, in structToLog);

        Assert.Equal(1, _logger.Collector.Count);
        var latestRecord = _logger.Collector.LatestRecord;
        Assert.Null(latestRecord.Exception);
        Assert.Equal("Testing nullable struct here...", latestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["structParam.LongProperty"] = structToLog.Value.LongProperty.ToInvariantString(),
            ["structParam.TransitiveStructProperty.DateTimeOffsetProperty"]
                = structToLog.Value.TransitiveStructProperty.DateTimeOffsetProperty.ToString(CultureInfo.InvariantCulture),

            ["structParam.NullableTransitiveStructProperty.DateTimeOffsetProperty"]
                = structToLog.Value.NullableTransitiveStructProperty.Value.DateTimeOffsetProperty.ToString(CultureInfo.InvariantCulture),

            ["structParam.NullableTransitiveStructProperty2.DateTimeOffsetProperty"]
                = structToLog.Value.NullableTransitiveStructProperty2?.DateTimeOffsetProperty.ToString(CultureInfo.InvariantCulture),

            ["{OriginalFormat}"] = "Testing nullable struct here..."
        };

        latestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void LogPropertyTestCustomExplicitNullableStruct()
    {
        LogMethodExplicitNullableStruct(_logger, null);

        Assert.Equal(1, _logger.Collector.Count);

        var latestRecord = _logger.Collector.LatestRecord;
        Assert.Null(latestRecord.Exception);
        Assert.Equal("Testing explicit nullable struct here...", latestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["structParam.LongProperty"] = null,
            ["structParam.TransitiveStructProperty.DateTimeOffsetProperty"] = null,
            ["structParam.NullableTransitiveStructProperty.DateTimeOffsetProperty"] = null,
            ["structParam.NullableTransitiveStructProperty2.DateTimeOffsetProperty"] = null,
            ["{OriginalFormat}"] = "Testing explicit nullable struct here..."
        };

        latestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void LogPropertiesDefaultAttrCtor()
    {
        var classToLog = new ClassAsParam { MyProperty = 0 };

        LogMethodDefaultAttrCtor(_logger, LogLevel.Critical, classToLog);

        Assert.Equal(1, _logger.Collector.Count);
        var latestRecord = _logger.Collector.LatestRecord;
        Assert.Null(latestRecord.Exception);
        Assert.NotEqual(0, latestRecord.Id.Id);
        Assert.Equal(LogLevel.Critical, latestRecord.Level);
        Assert.Equal(string.Empty, latestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["complexParam.MyProperty"] = classToLog.MyProperty.ToInvariantString()
        };

        latestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void LogPropertiesInterfaceArgument()
    {
        var classToLog = new MyInterfaceImpl
        {
            IntProperty = 100,
            ClassStringProperty = "test string", // won't get logged
            TransitiveProp = new LeafTransitiveBaseClass { IntegerProperty = 500 }
        };

        LogMethodInterfaceArg(_logger, classToLog);

        Assert.Equal(1, _logger.Collector.Count);
        var latestRecord = _logger.Collector.LatestRecord;
        Assert.Null(latestRecord.Exception);
        Assert.Equal(5, latestRecord.Id.Id);
        Assert.Equal(LogLevel.Information, latestRecord.Level);
        Assert.Equal("Testing interface-typed argument here...", latestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["complexParam.IntProperty"] = classToLog.IntProperty.ToInvariantString(),
            ["complexParam.TransitiveProp.IntegerProperty"] = classToLog.TransitiveProp.IntegerProperty.ToInvariantString(),
            ["{OriginalFormat}"] = "Testing interface-typed argument here..."
        };

        latestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void LogPropertiesRecordClassArgument()
    {
        var recordToLog = new MyRecordClass(100_500, "string_with_at_symbol - @");

        LogRecordClass(_logger, recordToLog);

        Assert.Equal(1, _logger.Collector.Count);
        var latestRecord = _logger.Collector.LatestRecord;
        Assert.Null(latestRecord.Exception);
        Assert.NotEqual(0, latestRecord.Id.Id);
        Assert.Equal(LogLevel.Debug, latestRecord.Level);
        Assert.Empty(latestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["p0.Value"] = recordToLog.Value.ToInvariantString(),
            ["p0.class"] = recordToLog.@class,
            ["p0.GetOnlyValue"] = recordToLog.GetOnlyValue.ToInvariantString(),
            ["p0.event"] = recordToLog.@event.ToString(CultureInfo.InvariantCulture)
        };

        latestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void LogPropertiesRecordStructArgument()
    {
        var recordToLog = new MyRecordStruct(100_500, "string value");

        LogRecordStruct(_logger, recordToLog);

        Assert.Equal(1, _logger.Collector.Count);
        var latestRecord = _logger.Collector.LatestRecord;
        Assert.Null(latestRecord.Exception);
        Assert.NotEqual(0, latestRecord.Id.Id);
        Assert.Equal(LogLevel.Debug, latestRecord.Level);
        Assert.Equal($"Struct is: {recordToLog}", latestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["p0"] = recordToLog.ToString(),
            ["p0.IntValue"] = recordToLog.IntValue.ToInvariantString(),
            ["p0.StringValue"] = recordToLog.StringValue,
            ["p0.GetOnlyValue"] = recordToLog.GetOnlyValue.ToInvariantString(),
            ["{OriginalFormat}"] = "Struct is: {p0}"
        };

        latestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void LogPropertiesReadonlyRecordStructArgument()
    {
        var recordToLog = new MyReadonlyRecordStruct(int.MaxValue, Guid.NewGuid().ToString());

        LogReadonlyRecordStruct(_logger, recordToLog);

        Assert.Equal(1, _logger.Collector.Count);
        var latestRecord = _logger.Collector.LatestRecord;
        Assert.Null(latestRecord.Exception);
        Assert.NotEqual(0, latestRecord.Id.Id);
        Assert.Equal(LogLevel.Debug, latestRecord.Level);
        Assert.Equal($"Readonly struct is: {recordToLog}", latestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["p0"] = recordToLog.ToString(),
            ["p0.IntValue"] = recordToLog.IntValue.ToInvariantString(),
            ["p0.StringValue"] = recordToLog.StringValue,
            ["p0.GetOnlyValue"] = recordToLog.GetOnlyValue.ToInvariantString(),
            ["{OriginalFormat}"] = "Readonly struct is: {p0}"
        };

        latestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }
}
