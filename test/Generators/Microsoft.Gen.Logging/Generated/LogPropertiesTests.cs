﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Numerics;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;
using Microsoft.Extensions.Telemetry.Testing.Logging;
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

        Assert.Equal("myProps_P5", _logger.LatestRecord.StructuredState![7].Key);
        Assert.Equal("[\"1\",\"2\",\"3\"]", _logger.LatestRecord.StructuredState[7].Value);

        Assert.Equal("myProps_P6", _logger.LatestRecord.StructuredState[8].Key);
        Assert.Equal("[\"4\",\"5\",\"6\"]", _logger.LatestRecord.StructuredState[8].Value);

        Assert.Equal("myProps_P7", _logger.LatestRecord.StructuredState[9].Key);
        Assert.Equal("{\"Seven\"=\"7\",\"Eight\"=\"8\",\"Nine\"=\"9\"}", _logger.LatestRecord.StructuredState[9].Value);
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
        Assert.Equal("P0", state[0].Key);
        Assert.Equal(props.P0.ToString(CultureInfo.InvariantCulture), state[0].Value);
        Assert.Equal("P1", state[1].Key);
        Assert.Equal(props.P1, state[1].Value);
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
        Assert.Equal("P0", state[0].Key);
        Assert.Equal(props.P0.ToString(CultureInfo.InvariantCulture), state[0].Value);
        Assert.Equal("P1", state[1].Key);
        Assert.Equal(props.P1, state[1].Value);
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

        Assert.Equal("p_P0", state[0].Key);
        Assert.Equal(props.P0.ToString(CultureInfo.InvariantCulture), state[0].Value);
        Assert.Equal("p_P1", state[1].Key);
        Assert.Equal(props.P1.ToString(CultureInfo.InvariantCulture), state[1].Value);
        Assert.Equal("p_P2", state[2].Key);
        Assert.Equal(props.P2.ToString(null, CultureInfo.InvariantCulture), state[2].Value);
        Assert.Equal("p_P3", state[3].Key);
        Assert.Equal(props.P3.ToString(), state[3].Value);
        Assert.Equal("p_P4", state[4].Key);
        Assert.Equal(props.P4.ToString(), state[4].Value);
        Assert.Equal("p_P5", state[5].Key);
        Assert.Equal(props.P5.ToString(), state[5].Value);
        Assert.Equal("p_P6", state[6].Key);
        Assert.Equal(props.P6.ToString(), state[6].Value);
        Assert.Equal("p_P7", state[7].Key);
        Assert.Equal(props.P7.ToString(), state[7].Value);
        Assert.Equal("p_P8", state[8].Key);
        Assert.Equal(props.P8.ToString(), state[8].Value);
        Assert.Equal("p_P9", state[9].Key);
        Assert.Equal(props.P9.ToString(), state[9].Value);
        Assert.Equal("p_P10", state[10].Key);
        Assert.Equal(props.P10.ToString(CultureInfo.InvariantCulture), state[10].Value);
        Assert.Equal("p_P11", state[11].Key);
        Assert.Equal(props.P11.ToString(CultureInfo.InvariantCulture), state[11].Value);
        Assert.Equal("p_P12", state[12].Key);
        Assert.Equal(props.P12.ToString(), state[12].Value);
        Assert.Equal("p_P13", state[13].Key);
        Assert.Equal(props.P13.ToString(), state[13].Value);
        Assert.Equal("p_P14", state[14].Key);
        Assert.Equal(props.P14.ToString(), state[14].Value);
        Assert.Equal("p_P15", state[15].Key);
        Assert.Equal(props.P15.ToString(), state[15].Value);
        Assert.Equal("p_P16", state[16].Key);
        Assert.Equal(props.P16.ToString(), state[16].Value);
        Assert.Equal("p_P17", state[17].Key);
        Assert.Equal(props.P17.ToString(), state[17].Value);
        Assert.Equal("p_P18", state[18].Key);
        Assert.Equal(props.P18.ToString(), state[18].Value);

#if NET6_0_OR_GREATER
        Assert.Equal("p_P19", state[19].Key);
        Assert.Equal(props.P19.ToString(CultureInfo.InvariantCulture), state[19].Value);
        Assert.Equal("p_P20", state[20].Key);
        Assert.Equal(props.P20.ToString(CultureInfo.InvariantCulture), state[20].Value);
#endif

    }

    [Fact]
    public void LogPropertiesNullHandling()
    {
        var provider = new StarRedactorProvider();

        var props = new LogPropertiesNullHandlingExtensions.MyProps
        {
            P0 = null!,
            P1 = null,
            P2 = 2,
            P3 = null,
        };

        LogPropertiesNullHandlingExtensions.M0(_logger, provider, props);
        Assert.Equal(5, _logger.LatestRecord.StructuredState!.Count);
        Assert.Equal("p_P0", _logger.LatestRecord.StructuredState[0].Key);
        Assert.Null(_logger.LatestRecord.StructuredState[0].Value);
        Assert.Equal("p_P1", _logger.LatestRecord.StructuredState[1].Key);
        Assert.Null(_logger.LatestRecord.StructuredState[1].Value);
        Assert.Equal("p_P2", _logger.LatestRecord.StructuredState[2].Key);
        Assert.Equal(props.P2.ToString(null, CultureInfo.InvariantCulture), _logger.LatestRecord.StructuredState[2].Value);
        Assert.Equal("p_P3", _logger.LatestRecord.StructuredState[3].Key);
        Assert.Null(_logger.LatestRecord.StructuredState[3].Value);
        Assert.Equal("p_P4", _logger.LatestRecord.StructuredState[4].Key);
        Assert.Null(_logger.LatestRecord.StructuredState[4].Value);
        Assert.Equal(1, _logger.Collector.Count);

        _logger.Collector.Clear();
        LogPropertiesNullHandlingExtensions.M1(_logger, provider, props);
        Assert.Equal(1, _logger.LatestRecord.StructuredState!.Count);
        Assert.Equal("p_P2", _logger.LatestRecord.StructuredState[0].Key);
        Assert.Equal(props.P2.ToString(null, CultureInfo.InvariantCulture), _logger.LatestRecord.StructuredState[0].Value);
        Assert.Equal(1, _logger.Collector.Count);
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
            ["classToLog_StringProperty"] = classToLog.StringProperty,
            ["classToLog_SimplifiedNullableIntProperty"] = null,
            ["classToLog_ExplicitNullableIntProperty"] = classToLog.ExplicitNullableIntProperty.ToString(),
            ["classToLog_GetOnlyProperty"] = classToLog.GetOnlyProperty.ToString(CultureInfo.InvariantCulture),
            ["classToLog_VirtualPropertyBase"] = classToLog.VirtualPropertyBase,
            ["classToLog_NonVirtualPropertyBase"] = classToLog.NonVirtualPropertyBase,
            ["classToLog_TransitivePropertyArray"] = LogMethodHelper.Stringify(classToLog.TransitivePropertyArray),
            ["classToLog_TransitiveProperty_TransitiveNumberProp"]
                = classToLog.TransitiveProperty.TransitiveNumberProp.ToString(CultureInfo.InvariantCulture),

            ["classToLog_TransitiveProperty_TransitiveStringProp"] = classToLog.TransitiveProperty.TransitiveStringProp,
            ["classToLog_TransitiveProperty_InnerTransitiveProperty_IntegerProperty"]
                = classToLog.TransitiveProperty.InnerTransitiveProperty.IntegerProperty.ToString(CultureInfo.InvariantCulture),
            ["classToLog_TransitiveProperty_InnerTransitiveProperty_DateTimeProperty"]
                = classToLog.TransitiveProperty.InnerTransitiveProperty.DateTimeProperty.ToString(CultureInfo.InvariantCulture),

            ["classToLog_AnotherTransitiveProperty_IntegerProperty"] = null, // Since AnotherTransitiveProperty is null
            ["classToLog_StringPropertyBase"] = classToLog.StringPropertyBase,
            ["classToLog_VirtualInterimProperty"] = classToLog.VirtualInterimProperty.ToInvariantString(),
            ["classToLog_InterimProperty"] = classToLog.InterimProperty.ToString(CultureInfo.InvariantCulture),
            ["classToLog_TransitiveProperty_TransitiveDerivedProp"] = classToLog.TransitiveProperty.TransitiveDerivedProp.ToInvariantString(),
            ["classToLog_TransitiveProperty_TransitiveVirtualProp"] = classToLog.TransitiveProperty.TransitiveVirtualProp.ToInvariantString(),
            ["classToLog_TransitiveProperty_TransitiveGenericProp_GenericProp"]
                = classToLog.TransitiveProperty.TransitiveGenericProp.GenericProp,

            ["classToLog_PropertyOfGenerics_GenericProp"] = classToLog.PropertyOfGenerics.GenericProp.ToInvariantString(),
            ["classToLog_CustomStructProperty_LongProperty"] = classToLog.CustomStructProperty.LongProperty.ToInvariantString(),
            ["classToLog_CustomStructProperty_TransitiveStructProperty_DateTimeOffsetProperty"]
                = classToLog.CustomStructProperty.TransitiveStructProperty.DateTimeOffsetProperty.ToString(CultureInfo.InvariantCulture),

            ["classToLog_CustomStructProperty_NullableTransitiveStructProperty_DateTimeOffsetProperty"]
                = classToLog.CustomStructProperty.NullableTransitiveStructProperty?.DateTimeOffsetProperty.ToString(CultureInfo.InvariantCulture),

            ["classToLog_CustomStructProperty_NullableTransitiveStructProperty2_DateTimeOffsetProperty"]
                = classToLog.CustomStructProperty.NullableTransitiveStructProperty2?.DateTimeOffsetProperty.ToString(CultureInfo.InvariantCulture),

            ["classToLog_CustomStructNullableProperty_LongProperty"] = classToLog.CustomStructNullableProperty?.LongProperty.ToInvariantString(),
            ["classToLog_CustomStructNullableProperty_TransitiveStructProperty_DateTimeOffsetProperty"]
                = classToLog.CustomStructNullableProperty?.TransitiveStructProperty.DateTimeOffsetProperty.ToString(CultureInfo.InvariantCulture),

            ["classToLog_CustomStructNullableProperty_NullableTransitiveStructProperty_DateTimeOffsetProperty"]
                = classToLog.CustomStructNullableProperty?.NullableTransitiveStructProperty?.DateTimeOffsetProperty.ToString(CultureInfo.InvariantCulture),

            ["classToLog_CustomStructNullableProperty_NullableTransitiveStructProperty2_DateTimeOffsetProperty"]
                = classToLog.CustomStructNullableProperty?.NullableTransitiveStructProperty2?.DateTimeOffsetProperty.ToString(CultureInfo.InvariantCulture),

            ["classToLog_CustomStructNullableProperty2_LongProperty"] = classToLog.CustomStructNullableProperty2?.LongProperty.ToInvariantString(),
            ["classToLog_CustomStructNullableProperty2_TransitiveStructProperty_DateTimeOffsetProperty"]
                = classToLog.CustomStructNullableProperty2?.TransitiveStructProperty.DateTimeOffsetProperty.ToString(CultureInfo.InvariantCulture),

            ["classToLog_CustomStructNullableProperty2_NullableTransitiveStructProperty_DateTimeOffsetProperty"]
                = classToLog.CustomStructNullableProperty2?.NullableTransitiveStructProperty?.DateTimeOffsetProperty.ToString(CultureInfo.InvariantCulture),

            ["classToLog_CustomStructNullableProperty2_NullableTransitiveStructProperty2_DateTimeOffsetProperty"]
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
            ["complexParam_MyProperty"] = classToLog.MyProperty.ToInvariantString(),
            ["{OriginalFormat}"] = "Both {StringProperty} and {ComplexParam} as params"
        };

        _logger.Collector.LatestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }

    [Fact]
    public void LogPropertiesNonStaticClassTest()
    {
        const string StringParamValue = "Value for a string";

        var classToLog = new ClassToLog { MyIntProperty = 0 };
        new NonStaticTestClass(_logger, null!).LogProperties(StringParamValue, classToLog);

        Assert.Equal(1, _logger.Collector.Count);
        var latestRecord = _logger.Collector.LatestRecord;
        Assert.Equal(LogLevel.Information, latestRecord.Level);
        Assert.Equal($"LogProperties: {StringParamValue}", latestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["P0"] = StringParamValue,
            ["p1_MyIntProperty"] = classToLog.MyIntProperty.ToInvariantString(),
            ["p1_MyStringProperty"] = classToLog.MyStringProperty,
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
            ["structParam_LongProperty"] = structToLog.LongProperty.ToInvariantString(),
            ["structParam_TransitiveStructProperty_DateTimeOffsetProperty"]
                = structToLog.TransitiveStructProperty.DateTimeOffsetProperty.ToString(CultureInfo.InvariantCulture),

            ["structParam_NullableTransitiveStructProperty_DateTimeOffsetProperty"]
                = structToLog.NullableTransitiveStructProperty?.DateTimeOffsetProperty.ToString(CultureInfo.InvariantCulture),

            ["structParam_NullableTransitiveStructProperty2_DateTimeOffsetProperty"]
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
            ["structParam_LongProperty"] = structToLog.Value.LongProperty.ToInvariantString(),
            ["structParam_TransitiveStructProperty_DateTimeOffsetProperty"]
                = structToLog.Value.TransitiveStructProperty.DateTimeOffsetProperty.ToString(CultureInfo.InvariantCulture),

            ["structParam_NullableTransitiveStructProperty_DateTimeOffsetProperty"]
                = structToLog.Value.NullableTransitiveStructProperty.Value.DateTimeOffsetProperty.ToString(CultureInfo.InvariantCulture),

            ["structParam_NullableTransitiveStructProperty2_DateTimeOffsetProperty"]
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
            ["structParam_LongProperty"] = null,
            ["structParam_TransitiveStructProperty_DateTimeOffsetProperty"] = null,
            ["structParam_NullableTransitiveStructProperty_DateTimeOffsetProperty"] = null,
            ["structParam_NullableTransitiveStructProperty2_DateTimeOffsetProperty"] = null,
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
        Assert.Equal(0, latestRecord.Id.Id);
        Assert.Equal(LogLevel.Critical, latestRecord.Level);
        Assert.Equal(string.Empty, latestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["complexParam_MyProperty"] = classToLog.MyProperty.ToInvariantString()
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
            ["complexParam_IntProperty"] = classToLog.IntProperty.ToInvariantString(),
            ["complexParam_TransitiveProp_IntegerProperty"] = classToLog.TransitiveProp.IntegerProperty.ToInvariantString(),
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
        Assert.Equal(0, latestRecord.Id.Id);
        Assert.Equal(LogLevel.Debug, latestRecord.Level);
        Assert.Empty(latestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["p0_Value"] = recordToLog.Value.ToInvariantString(),
            ["p0_class"] = recordToLog.@class,
            ["p0_GetOnlyValue"] = recordToLog.GetOnlyValue.ToInvariantString(),
            ["p0_event"] = recordToLog.@event.ToString(CultureInfo.InvariantCulture)
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
        Assert.Equal(0, latestRecord.Id.Id);
        Assert.Equal(LogLevel.Debug, latestRecord.Level);
        Assert.Equal($"Struct is: {recordToLog}", latestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["p0"] = recordToLog.ToString(),
            ["p0_IntValue"] = recordToLog.IntValue.ToInvariantString(),
            ["p0_StringValue"] = recordToLog.StringValue,
            ["p0_GetOnlyValue"] = recordToLog.GetOnlyValue.ToInvariantString(),
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
        Assert.Equal(0, latestRecord.Id.Id);
        Assert.Equal(LogLevel.Debug, latestRecord.Level);
        Assert.Equal($"Readonly struct is: {recordToLog}", latestRecord.Message);

        var expectedState = new Dictionary<string, string?>
        {
            ["p0"] = recordToLog.ToString(),
            ["p0_IntValue"] = recordToLog.IntValue.ToInvariantString(),
            ["p0_StringValue"] = recordToLog.StringValue,
            ["p0_GetOnlyValue"] = recordToLog.GetOnlyValue.ToInvariantString(),
            ["{OriginalFormat}"] = "Readonly struct is: {p0}"
        };

        latestRecord.StructuredState.Should().NotBeNull().And.Equal(expectedState);
    }
}
