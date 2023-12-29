// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using TestClasses;
using Xunit;

namespace Microsoft.Gen.Metrics.Test;

public partial class MetricTests
{
    [Fact]
    public void ThrowsOnNullStrongTypeObjectExt()
    {
        using (var collector = new MetricCollector<long>(_meter, "MyHistogramStrongTypeMetricExt"))
        {
            StrongTypeHistogramExt recorder = _meter.CreateHistogramExtStrongType();
            var ex = Assert.Throws<ArgumentNullException>(() => recorder.Record(4L, null!));
            Assert.NotNull(ex);
        }

        using (var collector = new MetricCollector<decimal>(_meter, "MyCounterStrongTypeMetricExt"))
        {
            StrongTypeDecimalCounterExt counter = _meter.CreateStrongTypeDecimalCounterExt();
            var ex = Assert.Throws<ArgumentNullException>(() => counter.Add(4M, null!));
            Assert.NotNull(ex);
        }
    }

    [Fact]
    public void NonGenericCounterExtNoDimsInstrumentTests()
    {
        using var collector = new MetricCollector<long>(_meter, nameof(CounterExt0D));
        CounterExt0D counter0D = _meter.CreateCounterExt0D();
        counter0D.Add(10L);
        counter0D.Add(5L);

        var measurements = collector.GetMeasurementSnapshot();
        Assert.Collection(measurements, x => Assert.Equal(10L, x.Value), x => Assert.Equal(5L, x.Value));
        Assert.All(measurements, x => Assert.Empty(x.Tags));
    }

    [Fact]
    public void NonGenericCounterExtInstrumentTests()
    {
        using var collector = new MetricCollector<long>(_meter, nameof(CounterExt2D));
        CounterExt2D counter2D = _meter.CreateCounterExt2D();
        counter2D.Add(11L, "val1", "val2");

        var measurement = Assert.Single(collector.GetMeasurementSnapshot());
        Assert.Equal(11L, measurement.Value);
        Assert.Equal(new (string, object?)[] { ("s1", "val1"), ("s2", "val2") }, measurement.Tags.Select(x => (x.Key, x.Value)));
    }

    [Fact]
    public void NonGenericHistogramExtNoDimsInstrumentTests()
    {
        using var collector = new MetricCollector<long>(_meter, nameof(HistogramExt0D));
        HistogramExt0D histogram0D = _meter.CreateHistogramExt0D();
        histogram0D.Record(12L);
        histogram0D.Record(6L);

        var measurements = collector.GetMeasurementSnapshot();
        Assert.Collection(measurements, x => Assert.Equal(12L, x.Value), x => Assert.Equal(6L, x.Value));
        Assert.All(measurements, x => Assert.Empty(x.Tags));
    }

    [Fact]
    public void NonGenericHistogramExtInstrumentTests()
    {
        using var collector = new MetricCollector<long>(_meter, nameof(HistogramExt1D));

        HistogramExt1D histogram1D = _meter.CreateHistogramExt1D();
        histogram1D.Record(17L, "val_1");

        var measurement = Assert.Single(collector.GetMeasurementSnapshot());
        Assert.Equal(17L, measurement.Value);
        var tag = Assert.Single(measurement.Tags);
        Assert.Equal(new KeyValuePair<string, object?>("s1", "val_1"), tag);
    }

    [Fact]
    public void GenericCounterExtNoDimsInstrumentTests()
    {
        using var collector = new MetricCollector<int>(_meter, nameof(GenericIntCounterExt0D));
        GenericIntCounterExt0D counter0D = _meter.CreateGenericIntCounterExt0D();
        counter0D.Add(10);
        counter0D.Add(5);

        var measurements = collector.GetMeasurementSnapshot();
        Assert.Collection(measurements, x => Assert.Equal(10, x.Value), x => Assert.Equal(5, x.Value));
        Assert.All(measurements, x => Assert.Empty(x.Tags));
    }

    [Fact]
    public void GenericCounterExtInstrumentTests()
    {
        using var collector = new MetricCollector<int>(_meter, nameof(GenericIntCounterExt1D));

        GenericIntCounterExt1D counter2D = _meter.CreateGenericIntCounterExt1D();
        counter2D.Add(11, "val1");

        var measurement = Assert.Single(collector.GetMeasurementSnapshot());
        Assert.Equal(11, measurement.Value);
        var tag = Assert.Single(measurement.Tags);
        Assert.Equal(new KeyValuePair<string, object?>("s1", "val1"), tag);
    }

    [Fact]
    public void GenericHistogramExtNoDimsInstrumentTests()
    {
        using var collector = new MetricCollector<int>(_meter, nameof(GenericIntHistogramExt0D));
        GenericIntHistogramExt0D histogram0D = _meter.CreateGenericIntHistogramExt0D();
        histogram0D.Record(12);
        histogram0D.Record(6);

        var measurements = collector.GetMeasurementSnapshot();
        Assert.Collection(measurements, x => Assert.Equal(12, x.Value), x => Assert.Equal(6, x.Value));
        Assert.All(measurements, x => Assert.Empty(x.Tags));
    }

    [Fact]
    public void GenericHistogramExtInstrumentTests()
    {
        using var collector = new MetricCollector<int>(_meter, nameof(GenericIntHistogramExt2D));

        GenericIntHistogramExt2D histogram1D = _meter.CreateGenericIntHistogramExt2D();
        histogram1D.Record(17, "val_1", "val_2");

        var measurement = Assert.Single(collector.GetMeasurementSnapshot());
        Assert.Equal(17, measurement.Value);
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

        using var collector = new MetricCollector<long>(_meter, "MyHistogramStrongTypeMetricExt");
        StrongTypeHistogramExt recorder = _meter.CreateHistogramExtStrongType();
        recorder.Record(1L, histogramDimensionsTest);

        var measurement = Assert.Single(collector.GetMeasurementSnapshot());
        Assert.Equal(1L, measurement.Value);
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
        collector.Clear();
        recorder.Record(2L, histogramDimensionsTest);

        measurement = Assert.Single(collector.GetMeasurementSnapshot());
        Assert.Equal(2L, measurement.Value);
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
        collector.Clear();
        recorder.Record(3L, histogramDimensionsTest);

        measurement = Assert.Single(collector.GetMeasurementSnapshot());
        Assert.Equal(3L, measurement.Value);
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

        using var collector = new MetricCollector<decimal>(_meter, "MyCounterStrongTypeMetricExt");
        StrongTypeDecimalCounterExt counter = _meter.CreateStrongTypeDecimalCounterExt();
        counter.Add(1M, counterDimensionsTest);

        var measurement = Assert.Single(collector.GetMeasurementSnapshot());
        Assert.Equal(1M, measurement.Value);
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
        collector.Clear();
        counter.Add(2M, counterDimensionsTest);

        measurement = Assert.Single(collector.GetMeasurementSnapshot());
        Assert.Equal(2M, measurement.Value);
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
        collector.Clear();
        counter.Add(3M, counterDimensionsTest);

        measurement = Assert.Single(collector.GetMeasurementSnapshot());
        Assert.Equal(3M, measurement.Value);
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
