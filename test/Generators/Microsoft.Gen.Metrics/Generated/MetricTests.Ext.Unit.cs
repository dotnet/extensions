// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using TestClasses;
using Xunit;
namespace Microsoft.Gen.Metrics.Test;

public partial class MetricTests
{
    [Fact]
    public void ValidateCounterWithUnit()
    {
        // Verify that a counter created with a unit works correctly
        using var collector = new MetricCollector<long>(_meter, "CounterWithUnit");

        // You'll need to add this to TestClasses/MetricsWithUnit.cs
        CounterWithUnit counter = MetricsWithUnit.CreateCounterWithUnit(_meter);
        counter.Add(100L);

        var measurement = Assert.Single(collector.GetMeasurementSnapshot());
        Assert.Equal(100L, measurement.Value);
        Assert.Empty(measurement.Tags);
        Assert.NotNull(collector.Instrument);
        Assert.Equal("seconds", collector.Instrument.Unit);
    }

    [Fact]
    public void ValidateHistogramWithUnit()
    {
        // Verify that a histogram created with a unit works correctly
        using var collector = new MetricCollector<long>(_meter, "HistogramWithUnit");

        // You'll need to add this to TestClasses/HistogramTestExtensions.cs
        HistogramWithUnit histogram = MetricsWithUnit.CreateHistogramWithUnit(_meter);
        histogram.Record(50L);

        var measurement = Assert.Single(collector.GetMeasurementSnapshot());
        Assert.Equal(50L, measurement.Value);
        Assert.Empty(measurement.Tags);

        Assert.NotNull(collector.Instrument);
        Assert.Equal("milliseconds", collector.Instrument.Unit);
    }

    [Fact]
    public void ValidateCounterWithUnitAndDimensions()
    {
        const long Value = 12345L;

        using var collector = new MetricCollector<long>(_meter, "CounterWithUnitAndDims");

        CounterWithUnitAndDims counter = MetricsWithUnit.CreateCounterWithUnitAndDims(_meter);
        counter.Add(Value, "dim1Value", "dim2Value");

        var measurement = Assert.Single(collector.GetMeasurementSnapshot());
        Assert.Equal(Value, measurement.Value);
        Assert.Equal(new (string, object?)[] { ("s1", "dim1Value"), ("s2", "dim2Value") },
            measurement.Tags.Select(x => (x.Key, x.Value)));

        // Verify the instrument has the correct unit
        Assert.NotNull(collector.Instrument);
        Assert.Equal("bytes", collector.Instrument.Unit);
    }

    [Fact]
    public void ValidateHistogramWithUnitAndDimensions()
    {
        const int Value = 9876;

        using var collector = new MetricCollector<int>(_meter, "HistogramWithUnitAndDims");

        HistogramWithUnitAndDims histogram = MetricsWithUnit.CreateHistogramWithUnitAndDims(_meter);
        histogram.Record(Value, "val1");

        var measurement = Assert.Single(collector.GetMeasurementSnapshot());
        Assert.Equal(Value, measurement.Value);
        var tag = Assert.Single(measurement.Tags);
        Assert.Equal(new KeyValuePair<string, object?>("s1", "val1"), tag);

        // Verify the instrument has the correct unit
        Assert.NotNull(collector.Instrument);
        Assert.Equal("requests", collector.Instrument.Unit);
    }

    [Fact]
    public void ValidateGenericCounterWithUnit()
    {
        using var collector = new MetricCollector<double>(_meter, "GenericDoubleCounterWithUnit");

        GenericDoubleCounterWithUnit counter = MetricsWithUnit.CreateGenericDoubleCounterWithUnit(_meter);
        counter.Add(3.14);

        var measurement = Assert.Single(collector.GetMeasurementSnapshot());
        Assert.Equal(3.14, measurement.Value);
        Assert.Empty(measurement.Tags);

        // Verify the instrument has the correct unit
        Assert.NotNull(collector.Instrument);
        Assert.Equal("meters", collector.Instrument.Unit);
    }

    [Fact]
    public void ValidateCounterWithEmptyUnit()
    {
        // Test that counters with empty/null units work
        using var collector = new MetricCollector<long>(_meter, nameof(Counter0D));
        Counter0D counter0D = CounterTestExtensions.CreateCounter0D(_meter);
        counter0D.Add(10L);

        var measurement = Assert.Single(collector.GetMeasurementSnapshot());
        Assert.Equal(10L, measurement.Value);

        // Verify the instrument has no unit (or default unit)
        Assert.NotNull(collector.Instrument);
        Assert.Null(collector.Instrument.Unit);
    }
}
