// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if false

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Diagnostics.Metrics;
using TestClasses;
using Xunit;

namespace Microsoft.Gen.Metrics.Test;

public partial class MetricTests : IDisposable
{
    private const string BaseMeterName = "Microsoft.GeneratedCode.Test.Metrics." + nameof(MetricTests) + ".";

    private readonly Meter _meter;
    private readonly FakeMetricsCollector _collector;
    private readonly string _meterName;
    private bool _disposedValue;

    public MetricTests()
    {
        _meterName = BaseMeterName + Guid.NewGuid().ToString("d", CultureInfo.InvariantCulture);
        _meter = new Meter(_meterName);
        _collector = new FakeMetricsCollector(new HashSet<string> { _meterName });
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _meter.Dispose();
                _collector.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void NonGenericCounter0DInstrumentTests()
    {
        Counter0D counter0D = CounterTestExtensions.CreateCounter0D(_meter);
        counter0D.Add(10L);
        counter0D.Add(5L);

        var measurements = _collector.GetSnapshot();
        Assert.Collection(measurements, x => Assert.Equal(10L, x.GetValueOrThrow<long>()), x => Assert.Equal(5L, x.GetValueOrThrow<long>()));
        Assert.All(measurements, x => Assert.Empty(x.Tags));
    }

    [Fact]
    public void NonGenericCounter2DInstrumentTests()
    {
        const long Value = int.MaxValue + 4L;

        Counter2D counter2D = CounterTestExtensions.CreateCounter2D(_meter);
        counter2D.Add(Value, "val1", "val2");

        var measurement = Assert.Single(_collector.GetSnapshot());
        Assert.Equal(Value, measurement.GetValueOrThrow<long>());
        Assert.Equal(new (string, object?)[] { ("s1", "val1"), ("s2", "val2") }, measurement.Tags.Select(x => (x.Key, x.Value)));
    }

    [Fact]
    public void NonGenericHistogram0DInstrumentTests()
    {
        Histogram0D histogram0D = HistogramTestExtensions.CreateHistogram0D(_meter);
        histogram0D.Record(12L);
        histogram0D.Record(6L);

        var measurements = _collector.GetSnapshot();
        Assert.Collection(measurements, x => Assert.Equal(12L, x.GetValueOrThrow<long>()), x => Assert.Equal(6L, x.GetValueOrThrow<long>()));
        Assert.All(measurements, x => Assert.Empty(x.Tags));
    }

    [Fact]
    public void NonGenericHistogram1DInstrumentTests()
    {
        const long Value = int.MaxValue + 3L;

        Histogram1D histogram1D = HistogramTestExtensions.CreateHistogram1D(_meter);
        histogram1D.Record(Value, "val_1");

        var measurement = Assert.Single(_collector.GetSnapshot());
        Assert.Equal(Value, measurement.GetValueOrThrow<long>());
        var tag = Assert.Single(measurement.Tags);
        Assert.Equal(new KeyValuePair<string, object?>("s1", "val_1"), tag);
    }

    [Fact]
    public void GenericCounter0DInstrumentTests()
    {
        GenericIntCounter0D counter0D = CounterTestExtensions.CreateGenericIntCounter0D(_meter);
        counter0D.Add(10);
        counter0D.Add(5);

        var measurements = _collector.GetSnapshot();
        Assert.Collection(measurements, x => Assert.Equal(10, x.GetValueOrThrow<int>()), x => Assert.Equal(5, x.GetValueOrThrow<int>()));
        Assert.All(measurements, x => Assert.Empty(x.Tags));
    }

    [Fact]
    public void GenericCounter2DInstrumentTests()
    {
        const int Value = int.MaxValue - 1;

        GenericIntCounter1D counter2D = CounterTestExtensions.CreateGenericIntCounter1D(_meter);
        counter2D.Add(Value, "val1");

        var measurement = Assert.Single(_collector.GetSnapshot());
        Assert.Equal(Value, measurement.GetValueOrThrow<int>());
        var tag = Assert.Single(measurement.Tags);
        Assert.Equal(new KeyValuePair<string, object?>("s1", "val1"), tag);
    }

    [Fact]
    public void GenericHistogram0DInstrumentTests()
    {
        GenericIntHistogram0D histogram0D = HistogramTestExtensions.CreateGenericIntHistogram0D(_meter);
        histogram0D.Record(12);
        histogram0D.Record(6);

        var measurements = _collector.GetSnapshot();
        Assert.Collection(measurements, x => Assert.Equal(12, x.GetValueOrThrow<int>()), x => Assert.Equal(6, x.GetValueOrThrow<int>()));
        Assert.All(measurements, x => Assert.Empty(x.Tags));
    }

    [Fact]
    public void GenericHistogram2DInstrumentTests()
    {
        const int Value = short.MaxValue + 2;

        GenericIntHistogram2D histogram1D = HistogramTestExtensions.CreateGenericIntHistogram2D(_meter);
        histogram1D.Record(Value, "val_1", "val_2");

        var measurement = Assert.Single(_collector.GetSnapshot());
        Assert.Equal(Value, measurement.GetValueOrThrow<int>());
        Assert.Equal(new (string, object?)[] { ("s1", "val_1"), ("s2", "val_2") }, measurement.Tags.Select(x => (x.Key, x.Value)));
    }

    [Fact]
    public void CreateOnExistingCounter()
    {
        const long Value = int.MaxValue + 2L;

        Counter4D counter4D = CounterTestExtensions.CreateCounter4D(_meter);
        Counter4D newCounter4D = CounterTestExtensions.CreateCounter4D(_meter);
        Assert.Same(counter4D, newCounter4D);

        counter4D.Add(Value, "val1", "val2", "val3", "val4");
        newCounter4D.Add(Value, "val3", "val4", "val5", "val6");

        Assert.Equal(2, _collector.Count);
        var measurements = _collector.GetSnapshot();
        Assert.All(measurements, x => Assert.Equal(Value, x.GetValueOrThrow<long>()));
        Assert.Same(measurements[0].Instrument, measurements[1].Instrument);

        var tags = measurements[0].Tags.Select(x => (x.Key, x.Value));
        Assert.Equal(new (string, object?)[] { ("s1", "val1"), ("s2", "val2"), ("s3", "val3"), ("s4", "val4") }, tags);

        tags = measurements[1].Tags.Select(x => (x.Key, x.Value));
        Assert.Equal(new (string, object?)[] { ("s1", "val3"), ("s2", "val4"), ("s3", "val5"), ("s4", "val6") }, tags);
    }

    [Fact]
    public void CreateOnExistingHistogram()
    {
        const long Value = int.MaxValue + 1L;

        Histogram4D histogram4D = HistogramTestExtensions.CreateHistogram4D(_meter);
        Histogram4D newHistogram4D = HistogramTestExtensions.CreateHistogram4D(_meter);
        Assert.Same(histogram4D, newHistogram4D);

        histogram4D.Record(Value, "val1", "val2", "val3", "val4");
        newHistogram4D.Record(Value, "val3", "val4", "val5", "val6");

        Assert.Equal(2, _collector.Count);
        var measurements = _collector.GetSnapshot();
        Assert.All(measurements, x => Assert.Equal(Value, x.GetValueOrThrow<long>()));
        Assert.Same(measurements[0].Instrument, measurements[1].Instrument);

        var tags = measurements[0].Tags.Select(x => (x.Key, x.Value));
        Assert.Equal(new (string, object?)[] { ("s1", "val1"), ("s2", "val2"), ("s3", "val3"), ("s4", "val4") }, tags);

        tags = measurements[1].Tags.Select(x => (x.Key, x.Value));
        Assert.Equal(new (string, object?)[] { ("s1", "val3"), ("s2", "val4"), ("s3", "val5"), ("s4", "val6") }, tags);
    }

    [Fact]
    public void CreateOnExistingCounter_WithDifferentMeterName_ShouldReturnNewMetric()
    {
        using var meter2 = new Meter(_meterName + "2");
        Counter3D counter = CounterTestExtensions.CreateCounter3D(_meter);

        // "Create()" with another meter name should return a different counter object
        Counter3D counterWithDifferentMeterName = CounterTestExtensions.CreateCounter3D(meter2);
        Assert.NotNull(counterWithDifferentMeterName);
        Assert.NotSame(counter, counterWithDifferentMeterName);

        Histogram3D histogram = HistogramTestExtensions.CreateHistogram3D(_meter);

        // "Create()" with another meter name should return a different histogram object
        Histogram3D histogramWithDifferentMeterName = HistogramTestExtensions.CreateHistogram3D(meter2);
        Assert.NotNull(histogramWithDifferentMeterName);
        Assert.NotSame(histogram, histogramWithDifferentMeterName);
    }

    [Fact]
    public void CreateOnExistingCounter_WithSameMeterName_ShouldReturnDifferentMetric()
    {
        using var meter2 = new Meter(_meterName);
        Counter3D counter = CounterTestExtensions.CreateCounter3D(_meter);

        // "Create()" with the same meter name should return a different counter object
        Counter3D counterWithSameMeterName = CounterTestExtensions.CreateCounter3D(meter2);
        Assert.NotNull(counterWithSameMeterName);
        Assert.NotSame(counter, counterWithSameMeterName);

        Histogram3D histogram3D = HistogramTestExtensions.CreateHistogram3D(_meter);

        // "Create()" with the same meter name should return a different histogram object
        Histogram3D histogramWithSameMeterName = HistogramTestExtensions.CreateHistogram3D(meter2);
        Assert.NotNull(histogramWithSameMeterName);
        Assert.NotSame(histogram3D, histogramWithSameMeterName);
    }

    [Fact]
    public void ValidateCounterWithDifferentDimensions()
    {
        Counter2D counter2D = CounterTestExtensions.CreateCounter2D(_meter);

        counter2D.Add(17L, "val1", "val2");

        var measurement = Assert.Single(_collector.GetSnapshot());
        Assert.Equal(17L, measurement.GetValueOrThrow<long>());
        Assert.Equal(new (string, object?)[] { ("s1", "val1"), ("s2", "val2") }, measurement.Tags.Select(x => (x.Key, x.Value)));
        _collector.Clear();

        counter2D.Add(5L, "val1", "val2");
        measurement = Assert.Single(_collector.GetSnapshot());
        Assert.Equal(5L, measurement.GetValueOrThrow<long>());
        Assert.Equal(new (string, object?)[] { ("s1", "val1"), ("s2", "val2") }, measurement.Tags.Select(x => (x.Key, x.Value)));
        _collector.Clear();

        // Different Dimensions
        counter2D.Add(5L, "val1", "val4");
        measurement = Assert.Single(_collector.GetSnapshot());
        Assert.Equal(5L, measurement.GetValueOrThrow<long>());
        Assert.Equal(new (string, object?)[] { ("s1", "val1"), ("s2", "val4") }, measurement.Tags.Select(x => (x.Key, x.Value)));
    }

    [Fact]
    public void ValidateHistogramWithDifferentDimensions()
    {
        Histogram2D histogram = HistogramTestExtensions.CreateHistogram2D(_meter);
        histogram.Record(10L, "val1", "val2");

        var measurement = Assert.Single(_collector.GetSnapshot());
        Assert.Equal(10L, measurement.GetValueOrThrow<long>());
        Assert.Equal(new (string, object?)[] { ("s1", "val1"), ("s2", "val2") }, measurement.Tags.Select(x => (x.Key, x.Value)));
        _collector.Clear();

        histogram.Record(5L, "val1", "val2");
        measurement = Assert.Single(_collector.GetSnapshot());
        Assert.Equal(5L, measurement.GetValueOrThrow<long>());
        Assert.Equal(new (string, object?)[] { ("s1", "val1"), ("s2", "val2") }, measurement.Tags.Select(x => (x.Key, x.Value)));
        _collector.Clear();

        // Different Dimensions
        histogram.Record(5L, "val1", "val4");
        measurement = Assert.Single(_collector.GetSnapshot());
        Assert.Equal(5L, measurement.GetValueOrThrow<long>());
        Assert.Equal(new (string, object?)[] { ("s1", "val1"), ("s2", "val4") }, measurement.Tags.Select(x => (x.Key, x.Value)));
    }

    [Fact]
    public void ValidateCounterWithFileScopedNamespace()
    {
        var longCunter = FileScopedExtensions.CreateCounter(_meter);
        longCunter.Add(12L);

        var measurement = Assert.Single(_collector.GetSnapshot());
        Assert.Equal(12L, measurement.GetValueOrThrow<long>());
        Assert.Empty(measurement.Tags);
        _collector.Clear();

        var genericDoubleCounter = FileScopedExtensions.CreateGenericDoubleCounter(_meter);
        genericDoubleCounter.Add(1.05D);

        measurement = Assert.Single(_collector.GetSnapshot());
        Assert.Equal(1.05D, measurement.GetValueOrThrow<double>());
        Assert.Empty(measurement.Tags);
    }

    [Fact]
    public void ValidateCounterWithVariableParamsDimensions()
    {
        CounterWithVariableParams counter = CounterTestExtensions.CreateCounterWithVariableParams(_meter);

        counter.Add(100_500L, Dim1: "val1", Dim_2: "val2", Dim_3: "val3");

        var measurement = Assert.Single(_collector.GetSnapshot());
        Assert.Equal(100_500L, measurement.GetValueOrThrow<long>());
        Assert.NotNull(measurement.Instrument);
        Assert.Equal("MyCounterMetric", measurement.Instrument.Name);
        Assert.Equal(new (string, object?)[] { ("Dim1", "val1"), ("Dim_2", "val2"), ("Dim_3", "val3") }, measurement.Tags.Select(x => (x.Key, x.Value)));
    }

    [Fact]
    public void ValidateHistogramWithVariableParamsDimensions()
    {
        HistogramWithVariableParams histogram = HistogramTestExtensions.CreateHistogramWithVariableParams(_meter);

        histogram.Record(100L, Dim1: "val1", Dim_2: "val2", Dim_3: "val3");

        var measurement = Assert.Single(_collector.GetSnapshot());
        Assert.Equal(100L, measurement.GetValueOrThrow<long>());
        Assert.NotNull(measurement.Instrument);
        Assert.Equal("MyHistogramMetric", measurement.Instrument.Name);
        Assert.Equal(new (string, object?)[] { ("Dim1", "val1"), ("Dim_2", "val2"), ("Dim_3", "val3") }, measurement.Tags.Select(x => (x.Key, x.Value)));
    }

    [Fact]
    public void ValidateHistogramStructType()
    {
        var histogramStruct = new HistogramStruct
        {
            Dim1 = "Dim1",
            Dim2 = "Dim2",
            DimInField = "Dim in field",
            Operations = HistogramOperations.Operation1,
            Operations2 = HistogramOperations.Operation1
        };

        StructTypeHistogram recorder = HistogramTestExtensions.CreateHistogramStructType(_meter);
        recorder.Record(10L, histogramStruct);

        var measurement = Assert.Single(_collector.GetSnapshot());
        Assert.Equal(10L, measurement.GetValueOrThrow<long>());
        Assert.NotNull(measurement.Instrument);
        Assert.Equal("MyHistogramStructTypeMetric", measurement.Instrument.Name);
        Assert.Equal(
            new (string, object?)[]
            {
                ("Dim1", histogramStruct.Dim1),
                ("DimInField", histogramStruct.DimInField),
                ("Dim2_FromAttribute", histogramStruct.Dim2),
                ("Operations", histogramStruct.Operations.ToString()),
                ("Operations_FromAttribute", histogramStruct.Operations2.ToString())
            },
            measurement.Tags.Select(x => (x.Key, x.Value)));
    }

    [Fact]
    public void ValidateHistogramStrongType()
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

        StrongTypeHistogram recorder = HistogramTestExtensions.CreateHistogramStrongType(_meter);
        recorder.Record(1L, histogramDimensionsTest);

        var measurement = Assert.Single(_collector.GetSnapshot());
        Assert.Equal(1L, measurement.GetValueOrThrow<long>());
        Assert.NotNull(measurement.Instrument);
        Assert.Equal("MyHistogramStrongTypeMetric", measurement.Instrument.Name);
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
        Assert.Equal("MyHistogramStrongTypeMetric", measurement.Instrument.Name);
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
        Assert.Equal("MyHistogramStrongTypeMetric", measurement.Instrument.Name);
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
    public void ThrowsOnNullStrongTypeObject()
    {
        StrongTypeHistogram recorder = HistogramTestExtensions.CreateHistogramStrongType(_meter);
        var ex = Assert.Throws<ArgumentNullException>(() => recorder.Record(4L, null!));
        Assert.NotNull(ex);

        StrongTypeDecimalCounter counter = CounterTestExtensions.CreateStrongTypeDecimalCounter(_meter);
        ex = Assert.Throws<ArgumentNullException>(() => counter.Add(4M, null!));
        Assert.NotNull(ex);
    }

    [Fact]
    public void ValidateCounterStructType()
    {
        var counterStruct = new CounterStructDimensions
        {
            Dim1 = "Dim1",
            Dim2 = "Dim2",
            DimInField = "Dim in field",
            Operations = CounterOperations.Operation1,
            Operations2 = CounterOperations.Operation1
        };

        StructTypeCounter recorder = CounterTestExtensions.CreateCounterStructType(_meter);
        recorder.Add(11L, counterStruct);

        var measurement = Assert.Single(_collector.GetSnapshot());
        Assert.Equal(11L, measurement.GetValueOrThrow<long>());
        Assert.NotNull(measurement.Instrument);
        Assert.Equal("MyCounterStructTypeMetric", measurement.Instrument.Name);
        Assert.Equal(
            new (string, object?)[]
            {
                ("Dim1", counterStruct.Dim1),
                ("DimInField", counterStruct.DimInField),
                ("Dim2_FromAttribute", counterStruct.Dim2),
                ("Operations", counterStruct.Operations.ToString()),
                ("Operations_FromAttribute", counterStruct.Operations2.ToString())
            },
            measurement.Tags.Select(x => (x.Key, x.Value)));
    }

    [Fact]
    public void ValidateCounterStrongType()
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

        StrongTypeDecimalCounter counter = CounterTestExtensions.CreateStrongTypeDecimalCounter(_meter);
        counter.Add(1M, counterDimensionsTest);

        var measurement = Assert.Single(_collector.GetSnapshot());
        Assert.Equal(1M, measurement.GetValueOrThrow<decimal>());
        Assert.NotNull(measurement.Instrument);
        Assert.Equal("MyCounterStrongTypeMetric", measurement.Instrument.Name);
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
        Assert.Equal("MyCounterStrongTypeMetric", measurement.Instrument.Name);
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
        Assert.Equal("MyCounterStrongTypeMetric", measurement.Instrument.Name);
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
