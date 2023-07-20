// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if false

using System;
using System.Collections.Generic;
using System.Linq;
using TestClasses;
using Xunit;

namespace Microsoft.Gen.Metering.Test;

public partial class MetricTests
{
    [Fact]
    public void ThrowsOnNullStrongTypeObjectExt()
    {
        StrongTypeHistogramExt recorder = _meter.CreateHistogramExtStrongType();
        var ex = Assert.Throws<ArgumentNullException>(() => recorder.Record(4L, null!));
        Assert.NotNull(ex);

        StrongTypeDecimalCounterExt counter = _meter.CreateStrongTypeDecimalCounterExt();
        ex = Assert.Throws<ArgumentNullException>(() => counter.Add(4M, null!));
        Assert.NotNull(ex);
    }

    [Fact]
    public void NonGenericCounterExtInstrumentTests()
    {
        CounterExt0D counter0D = _meter.CreateCounterExt0D();
        counter0D.Add(10L);
        counter0D.Add(5L);

        var measurements = _collector.GetSnapshot();
        Assert.Collection(measurements, x => Assert.Equal(10L, x.GetValueOrThrow<long>()), x => Assert.Equal(5L, x.GetValueOrThrow<long>()));
        Assert.All(measurements, x => Assert.Empty(x.Tags));
        _collector.Clear();

        CounterExt2D counter2D = _meter.CreateCounterExt2D();
        counter2D.Add(11L, "val1", "val2");

        var measurement = Assert.Single(_collector.GetSnapshot());
        Assert.Equal(11L, measurement.GetValueOrThrow<long>());
        Assert.Equal(new (string, object?)[] { ("s1", "val1"), ("s2", "val2") }, measurement.Tags.Select(x => (x.Key, x.Value)));
    }

    [Fact]
    public void NonGenericHistogramExtInstrumentTests()
    {
        HistogramExt0D histogram0D = _meter.CreateHistogramExt0D();
        histogram0D.Record(12L);
        histogram0D.Record(6L);

        var measurements = _collector.GetSnapshot();
        Assert.Collection(measurements, x => Assert.Equal(12L, x.GetValueOrThrow<long>()), x => Assert.Equal(6L, x.GetValueOrThrow<long>()));
        Assert.All(measurements, x => Assert.Empty(x.Tags));
        _collector.Clear();

        HistogramExt1D histogram1D = _meter.CreateHistogramExt1D();
        histogram1D.Record(17L, "val_1");

        var measurement = Assert.Single(_collector.GetSnapshot());
        Assert.Equal(17L, measurement.GetValueOrThrow<long>());
        var tag = Assert.Single(measurement.Tags);
        Assert.Equal(new KeyValuePair<string, object?>("s1", "val_1"), tag);
    }

    [Fact]
    public void GenericCounterExtInstrumentTests()
    {
        GenericIntCounterExt0D counter0D = _meter.CreateGenericIntCounterExt0D();
        counter0D.Add(10);
        counter0D.Add(5);

        var measurements = _collector.GetSnapshot();
        Assert.Collection(measurements, x => Assert.Equal(10, x.GetValueOrThrow<int>()), x => Assert.Equal(5, x.GetValueOrThrow<int>()));
        Assert.All(measurements, x => Assert.Empty(x.Tags));
        _collector.Clear();

        GenericIntCounterExt1D counter2D = _meter.CreateGenericIntCounterExt1D();
        counter2D.Add(11, "val1");

        var measurement = Assert.Single(_collector.GetSnapshot());
        Assert.Equal(11, measurement.GetValueOrThrow<int>());
        var tag = Assert.Single(measurement.Tags);
        Assert.Equal(new KeyValuePair<string, object?>("s1", "val1"), tag);
    }

    [Fact]
    public void GenericHistogramExtInstrumentTests()
    {
        GenericIntHistogramExt0D histogram0D = _meter.CreateGenericIntHistogramExt0D();
        histogram0D.Record(12);
        histogram0D.Record(6);

        var measurements = _collector.GetSnapshot();
        Assert.Collection(measurements, x => Assert.Equal(12, x.GetValueOrThrow<int>()), x => Assert.Equal(6, x.GetValueOrThrow<int>()));
        Assert.All(measurements, x => Assert.Empty(x.Tags));
        _collector.Clear();

        GenericIntHistogramExt2D histogram1D = _meter.CreateGenericIntHistogramExt2D();
        histogram1D.Record(17, "val_1", "val_2");

        var measurement = Assert.Single(_collector.GetSnapshot());
        Assert.Equal(17, measurement.GetValueOrThrow<int>());
        Assert.Equal(new (string, object?)[] { ("s1", "val_1"), ("s2", "val_2") }, measurement.Tags.Select(x => (x.Key, x.Value)));
    }

    [Fact]
    public void ValidateHistogramExtStrongType()
    {
        var histogramDimensionsTest = new HistogramDimensionsTest
        {
            Dim1 = "Dim1",
            OperationsEnum = HistogramOperations.Operation1,
            OperationsEnum2 = HistogramOperations.Operation1,
            ParentOperationName = "ParentOperationName",
            ChildDimensionsObject = new HistogramChildDimensions
            {
                Dim2 = "Dim2",
                SomeDim = "SomeDime"
            },
            ChildDimensionsStruct = new HistogramDimensionsStruct
            {
                Dim4Struct = "Dim4",
                Dim5Struct = "Dim5"
            },
            GrandChildrenDimensionsObject = new HistogramGrandChildrenDimensions
            {
                Dim3 = "Dim3",
                SomeDim = "SomeDim"
            }
        };

        StrongTypeHistogramExt recorder = _meter.CreateHistogramExtStrongType();
        recorder.Record(1L, histogramDimensionsTest);

        var measurement = Assert.Single(_collector.GetSnapshot());
        Assert.Equal(1L, measurement.GetValueOrThrow<long>());
        Assert.NotNull(measurement.Instrument);
        Assert.Equal("MyHistogramStrongTypeMetricExt", measurement.Instrument.Name);
        Assert.Equal(
            new (string, object?)[]
            {
                ("Dim1",                histogramDimensionsTest.Dim1),
                ("OperationsEnum",      histogramDimensionsTest.OperationsEnum.ToString()),
                ("Enum2",               histogramDimensionsTest.OperationsEnum2.ToString()),
                ("Dim2",                histogramDimensionsTest.ChildDimensionsObject.Dim2),
                ("dim2FromAttribute",   histogramDimensionsTest.ChildDimensionsObject.SomeDim),
                ("Dim3",                histogramDimensionsTest.GrandChildrenDimensionsObject.Dim3),
                ("Dim3FromAttribute",   histogramDimensionsTest.GrandChildrenDimensionsObject.SomeDim),
                ("ParentOperationName", histogramDimensionsTest.ParentOperationName),
                ("Dim4Struct",          histogramDimensionsTest.ChildDimensionsStruct.Dim4Struct),
                ("Dim5FromAttribute",   histogramDimensionsTest.ChildDimensionsStruct.Dim5Struct)
            },
            measurement.Tags.Select(x => (x.Key, x.Value)));

        histogramDimensionsTest.ChildDimensionsObject = null!;
        _collector.Clear();
        recorder.Record(2L, histogramDimensionsTest);

        measurement = Assert.Single(_collector.GetSnapshot());
        Assert.Equal(2L, measurement.GetValueOrThrow<long>());
        Assert.NotNull(measurement.Instrument);
        Assert.Equal("MyHistogramStrongTypeMetricExt", measurement.Instrument.Name);
        Assert.Equal(
            new (string, object?)[]
            {
                ("Dim1",                histogramDimensionsTest.Dim1),
                ("OperationsEnum",      histogramDimensionsTest.OperationsEnum.ToString()),
                ("Enum2",               histogramDimensionsTest.OperationsEnum2.ToString()),
                ("Dim2",                null),
                ("dim2FromAttribute",   null),
                ("Dim3",                histogramDimensionsTest.GrandChildrenDimensionsObject.Dim3),
                ("Dim3FromAttribute",   histogramDimensionsTest.GrandChildrenDimensionsObject.SomeDim),
                ("ParentOperationName", histogramDimensionsTest.ParentOperationName),
                ("Dim4Struct",          histogramDimensionsTest.ChildDimensionsStruct.Dim4Struct),
                ("Dim5FromAttribute",   histogramDimensionsTest.ChildDimensionsStruct.Dim5Struct)
            },
            measurement.Tags.Select(x => (x.Key, x.Value)));

        histogramDimensionsTest.GrandChildrenDimensionsObject = null!;
        _collector.Clear();
        recorder.Record(3L, histogramDimensionsTest);

        measurement = Assert.Single(_collector.GetSnapshot());
        Assert.Equal(3L, measurement.GetValueOrThrow<long>());
        Assert.NotNull(measurement.Instrument);
        Assert.Equal("MyHistogramStrongTypeMetricExt", measurement.Instrument.Name);
        Assert.Equal(
            new (string, object?)[]
            {
                ("Dim1",                histogramDimensionsTest.Dim1),
                ("OperationsEnum",      histogramDimensionsTest.OperationsEnum.ToString()),
                ("Enum2",               histogramDimensionsTest.OperationsEnum2.ToString()),
                ("Dim2",                null),
                ("dim2FromAttribute",   null),
                ("Dim3",                null),
                ("Dim3FromAttribute",   null),
                ("ParentOperationName", histogramDimensionsTest.ParentOperationName),
                ("Dim4Struct",          histogramDimensionsTest.ChildDimensionsStruct.Dim4Struct),
                ("Dim5FromAttribute",   histogramDimensionsTest.ChildDimensionsStruct.Dim5Struct)
            },
            measurement.Tags.Select(x => (x.Key, x.Value)));
    }

    [Fact]
    public void ValidateCounterExtStrongType()
    {
        var counterDimensionsTest = new CounterDimensions
        {
            OperationsEnum = CounterOperations.Operation1,
            OperationsEnum2 = CounterOperations.Operation1,
            ParentOperationName = "ParentOperationName",
            ChildDimensionsObject = new CounterChildDimensions
            {
                Dim2 = "Dim2",
                SomeDim = "SomeDime"
            },
            ChildDimensionsStruct = new CounterDimensionsStruct
            {
                Dim4Struct = "Dim4",
                Dim5Struct = "Dim5"
            },
            GrandChildDimensionsObject = new CounterGrandChildCounterDimensions
            {
                Dim3 = "Dim3",
                SomeDim = "SomeDim"
            },
            Dim1 = "Dim1",
        };

        StrongTypeDecimalCounterExt counter = _meter.CreateStrongTypeDecimalCounterExt();
        counter.Add(1M, counterDimensionsTest);

        var measurement = Assert.Single(_collector.GetSnapshot());
        Assert.Equal(1M, measurement.GetValueOrThrow<decimal>());
        Assert.NotNull(measurement.Instrument);
        Assert.Equal("MyCounterStrongTypeMetricExt", measurement.Instrument.Name);
        Assert.Equal(
            new (string, object?)[]
            {
                ("Dim1",                counterDimensionsTest.Dim1),
                ("OperationsEnum",      counterDimensionsTest.OperationsEnum.ToString()),
                ("Enum2",               counterDimensionsTest.OperationsEnum2.ToString()),
                ("Dim2",                counterDimensionsTest.ChildDimensionsObject.Dim2),
                ("dim2FromAttribute",   counterDimensionsTest.ChildDimensionsObject.SomeDim),
                ("Dim3",                counterDimensionsTest.GrandChildDimensionsObject.Dim3),
                ("Dim3FromAttribute",   counterDimensionsTest.GrandChildDimensionsObject.SomeDim),
                ("ParentOperationName", counterDimensionsTest.ParentOperationName),
                ("Dim4Struct",          counterDimensionsTest.ChildDimensionsStruct.Dim4Struct),
                ("Dim5FromAttribute",   counterDimensionsTest.ChildDimensionsStruct.Dim5Struct)
            },
            measurement.Tags.Select(x => (x.Key, x.Value)));

        counterDimensionsTest.ChildDimensionsObject = null!;
        _collector.Clear();
        counter.Add(2M, counterDimensionsTest);

        measurement = Assert.Single(_collector.GetSnapshot());
        Assert.Equal(2M, measurement.GetValueOrThrow<decimal>());
        Assert.NotNull(measurement.Instrument);
        Assert.Equal("MyCounterStrongTypeMetricExt", measurement.Instrument.Name);
        Assert.Equal(
            new (string, object?)[]
            {
                ("Dim1",                counterDimensionsTest.Dim1),
                ("OperationsEnum",      counterDimensionsTest.OperationsEnum.ToString()),
                ("Enum2",               counterDimensionsTest.OperationsEnum2.ToString()),
                ("Dim2",                null),
                ("dim2FromAttribute",   null),
                ("Dim3",                counterDimensionsTest.GrandChildDimensionsObject.Dim3),
                ("Dim3FromAttribute",   counterDimensionsTest.GrandChildDimensionsObject.SomeDim),
                ("ParentOperationName", counterDimensionsTest.ParentOperationName),
                ("Dim4Struct",          counterDimensionsTest.ChildDimensionsStruct.Dim4Struct),
                ("Dim5FromAttribute",   counterDimensionsTest.ChildDimensionsStruct.Dim5Struct)
            },
            measurement.Tags.Select(x => (x.Key, x.Value)));

        counterDimensionsTest.GrandChildDimensionsObject = null!;
        _collector.Clear();
        counter.Add(3M, counterDimensionsTest);

        measurement = Assert.Single(_collector.GetSnapshot());
        Assert.Equal(3M, measurement.GetValueOrThrow<decimal>());
        Assert.NotNull(measurement.Instrument);
        Assert.Equal("MyCounterStrongTypeMetricExt", measurement.Instrument.Name);
        Assert.Equal(
            new (string, object?)[]
            {
                ("Dim1",                counterDimensionsTest.Dim1),
                ("OperationsEnum",      counterDimensionsTest.OperationsEnum.ToString()),
                ("Enum2",               counterDimensionsTest.OperationsEnum2.ToString()),
                ("Dim2",                null),
                ("dim2FromAttribute",   null),
                ("Dim3",                null),
                ("Dim3FromAttribute",   null),
                ("ParentOperationName", counterDimensionsTest.ParentOperationName),
                ("Dim4Struct",          counterDimensionsTest.ChildDimensionsStruct.Dim4Struct),
                ("Dim5FromAttribute",   counterDimensionsTest.ChildDimensionsStruct.Dim5Struct)
            },
            measurement.Tags.Select(x => (x.Key, x.Value)));
    }
}

#endif
