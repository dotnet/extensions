// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Telemetry.Testing.Metering.Internal;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Testing.Metering.Test;

public class MetricValuesHolderTests
{
    [Fact]
    public void MetricName_MatchesInstrumentName()
    {
        using var meter = new Meter(Guid.NewGuid().ToString());
        using var metricCollector = new MetricCollector(meter);

        var counter = meter.CreateCounter<int>(Guid.NewGuid().ToString());
        var counterValuesHolder = metricCollector.GetCounterValues<int>(counter.Name);

        Assert.NotNull(counterValuesHolder);
        Assert.Equal(counterValuesHolder.Instrument.Name, counter.Name);

        var histogram = meter.CreateHistogram<int>(Guid.NewGuid().ToString());
        var histgramValuesHolder = metricCollector.GetHistogramValues<int>(histogram.Name);

        Assert.NotNull(histgramValuesHolder);
        Assert.Equal(histgramValuesHolder.Instrument.Name, histogram.Name);

        var updownCounter = meter.CreateUpDownCounter<int>(Guid.NewGuid().ToString());
        var updownCounterValuesHolder = metricCollector.GetUpDownCounterValues<int>(updownCounter.Name);

        Assert.NotNull(updownCounterValuesHolder);
        Assert.Equal(updownCounterValuesHolder.Instrument.Name, updownCounter.Name);
    }

    [Fact]
    public void Count()
    {
        using var meter = new Meter(Guid.NewGuid().ToString());
        using var metricCollector = new MetricCollector(meter);

        var counter = meter.CreateCounter<int>(Guid.NewGuid().ToString());
        var counterValuesHolder = metricCollector.GetCounterValues<int>(counter.Name);
        counter.Add(1);
        Assert.Equal(1, counterValuesHolder!.Count);

        var histogram = meter.CreateHistogram<int>(Guid.NewGuid().ToString());
        var histgramValuesHolder = metricCollector.GetHistogramValues<int>(histogram.Name);
        histogram.Record(1);
        histogram.Record(1);
        Assert.Equal(2, histgramValuesHolder!.Count);

        var updownCounter = meter.CreateUpDownCounter<int>(Guid.NewGuid().ToString());
        var updownCounterValuesHolder = metricCollector.GetUpDownCounterValues<int>(updownCounter.Name);
        Assert.Equal(0, updownCounterValuesHolder!.Count);
    }

    [Fact]
    public void LastWrittenValue_ReturnsTheLatestMeasurementValue()
    {
        using var meter = new Meter(Guid.NewGuid().ToString());
        using var metricCollector = new MetricCollector(meter);

        var counter = meter.CreateCounter<int>(Guid.NewGuid().ToString());
        var counterValuesHolder = metricCollector.GetCounterValues<int>(counter.Name)!;

        Assert.Null(counterValuesHolder.LatestWrittenValue);

        const int CounterValue = 1;
        counter.Add(CounterValue);

        Assert.Equal(CounterValue, counterValuesHolder.LatestWrittenValue!.Value);

        var histogram = meter.CreateHistogram<int>(Guid.NewGuid().ToString());
        var histogramValuesHolder = metricCollector.GetHistogramValues<int>(histogram.Name)!;

        Assert.Null(histogramValuesHolder.LatestWrittenValue);

        var testValues = new[] { 20, 40, 60, 100, 200, 1000, 5000 };

        foreach (var testValue in testValues)
        {
            histogram.Record(testValue);

            Assert.Equal(testValue, histogramValuesHolder.LatestWrittenValue!.Value);
        }
    }

    [Fact]
    public void LastWritten_ReturnsTheMetricValue()
    {
        using var meter = new Meter(Guid.NewGuid().ToString());
        using var metricCollector = new MetricCollector(meter);

        var counter = meter.CreateCounter<int>(Guid.NewGuid().ToString());
        var counterValuesHolder = metricCollector.GetCounterValues<int>(counter.Name)!;

        Assert.Null(counterValuesHolder.LatestWritten);

        const int CounterValue = 1;
        counter.Add(CounterValue);

        Assert.Equal(CounterValue, counterValuesHolder.LatestWritten!.Value);

        var histogram = meter.CreateHistogram<int>(Guid.NewGuid().ToString());
        var histogramValuesHolder = metricCollector.GetHistogramValues<int>(histogram.Name)!;

        Assert.Null(histogramValuesHolder.LatestWritten);

        const int Value1 = 111;
        histogram.Record(Value1);

        var metricValue1 = histogramValuesHolder.LatestWritten;

        Assert.NotNull(metricValue1);
        Assert.Equal(Value1, metricValue1.Value);
        Assert.Empty(metricValue1.Tags);

        const int Value2 = 2222;
        var dimension21 = new KeyValuePair<string, object?>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        histogram.Record(Value2, dimension21);

        var metricValue2 = histogramValuesHolder.LatestWritten;

        Assert.NotNull(metricValue2);
        Assert.Equal(Value2, metricValue2.Value);
        Assert.Equal(new[] { new KeyValuePair<string, object?>(dimension21.Key, dimension21.Value) }, metricValue2.Tags);

        const int Value3 = 9991;
        var dimension31 = new KeyValuePair<string, object?>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        var dimension32 = new KeyValuePair<string, object?>(Guid.NewGuid().ToString(), 1);
        var dimension33 = new KeyValuePair<string, object?>(Guid.NewGuid().ToString(), 'c');
        var dimension34 = new KeyValuePair<string, object?>(Guid.NewGuid().ToString(), null);
        histogram.Record(Value3, dimension31, dimension32, dimension33, dimension34);

        var metricValue3 = histogramValuesHolder.LatestWritten;

        var expectedTags = new[]
            {
                new KeyValuePair<string, object?>(dimension31.Key, dimension31.Value),
                new KeyValuePair<string, object?>(dimension32.Key, dimension32.Value),
                new KeyValuePair<string, object?>(dimension33.Key, dimension33.Value),
                new KeyValuePair<string, object?>(dimension34.Key, dimension34.Value),
            };
        Array.Sort(expectedTags, (x, y) => StringComparer.OrdinalIgnoreCase.Compare(x.Key, y.Key));

        Assert.NotNull(metricValue3);
        Assert.Equal(Value3, metricValue3.Value);
        Assert.Equal(expectedTags, metricValue3.Tags);
    }

    [Fact]
    public void ReceiveValue_ThrowsWhenInvalidDimensionValue()
    {
        using var meter = new Meter(Guid.NewGuid().ToString());
        var instrument = meter.CreateCounter<int>(Guid.NewGuid().ToString());
        var metricValuesHolder = new MetricValuesHolder<int>(TimeProvider.System, AggregationType.Save, instrument);

        var ex = Assert.Throws<InvalidOperationException>(() => metricValuesHolder.ReceiveValue(int.MaxValue, new[] { new KeyValuePair<string, object?>("Dimension1", new object()) }));
        Assert.Equal($"The type {typeof(object).FullName} is not supported as a dimension value type.", ex.Message);

        var ex1 = Assert.Throws<InvalidOperationException>(() => metricValuesHolder.ReceiveValue(int.MinValue, new[] { new KeyValuePair<string, object?>("Dimension2", new[] { new object() }) }));
        Assert.Equal($"The type {typeof(object[]).FullName} is not supported as a dimension value type.", ex1.Message);
    }

    [Fact]
    public void GetValue_ReturnsCapturedMeasurementValue()
    {
        using var meter = new Meter(Guid.NewGuid().ToString());
        var instrument = meter.CreateCounter<int>(Guid.NewGuid().ToString());
        var metricValuesHolder = new MetricValuesHolder<int>(System.TimeProvider.System, AggregationType.Save, instrument);

        const int Value1 = 1;
        metricValuesHolder.ReceiveValue(Value1, null);

        Assert.Equal(Value1, metricValuesHolder.GetValue());

        const int Value2 = 20;
        metricValuesHolder.ReceiveValue(Value2, null);

        Assert.Equal(Value2, metricValuesHolder.GetValue());
    }

    [Fact]
    public void ReceiveValue_TimestampIsRecorded()
    {
        var recordTime = DateTimeOffset.UtcNow.AddDays(-1);
        var timeProvider = new FakeTimeProvider(recordTime);
        using var meter = new Meter(Guid.NewGuid().ToString());
        var instrument = meter.CreateCounter<int>(Guid.NewGuid().ToString());
        var metricValuesHolder = new MetricValuesHolder<int>(timeProvider, AggregationType.Save, instrument);

        metricValuesHolder.ReceiveValue(50, null);

        Assert.NotNull(metricValuesHolder.LatestWrittenValue);
        Assert.Equal(recordTime, metricValuesHolder.LatestWritten!.Timestamp);
    }

    [Fact]
    public void GetDimension_RetursDimensionValue()
    {
        using var meter = new Meter(Guid.NewGuid().ToString());
        var instrument = meter.CreateCounter<int>(Guid.NewGuid().ToString());
        var metricValuesHolder = new MetricValuesHolder<int>(TimeProvider.System, AggregationType.Save, instrument);

        var intDimension = "int_dimension";
        int intVal = 11111;
        var stringDimension = "string_dimension";
        var stringValue = Guid.NewGuid().ToString();
        var nullDimension = "null_dimension";
        var doubleDimension = "double_dimension";
        var doubleVal = 78.78d;

        var dimensions = new[]
        {
            new KeyValuePair<string, object?>(intDimension, intVal),
            new KeyValuePair<string, object?>(stringDimension, stringValue),
            new KeyValuePair<string, object?>(nullDimension, null),
            new KeyValuePair<string, object?>(doubleDimension, doubleVal)
        };

        metricValuesHolder.ReceiveValue(50, dimensions);

        Assert.NotNull(metricValuesHolder.LatestWritten);
        Assert.Equal(intVal, metricValuesHolder.LatestWritten.GetDimension(intDimension));
        Assert.Equal(stringValue, metricValuesHolder.LatestWritten.GetDimension(stringDimension));
        Assert.Equal(doubleVal, metricValuesHolder.LatestWritten.GetDimension(doubleDimension));
        Assert.Null(metricValuesHolder.LatestWritten.GetDimension(nullDimension));
        Assert.Null(metricValuesHolder.LatestWritten.GetDimension("invalid_dimension_name"));
    }

    [Fact]
    public void ReceiveValue_ThrowsWhenInvalidAggregationTypeIsUsed()
    {
        using var meter = new Meter(Guid.NewGuid().ToString());
        var instrument = meter.CreateCounter<int>(Guid.NewGuid().ToString());
        AggregationType invalidAggregationType = (AggregationType)111;
        var metricValuesHolder = new MetricValuesHolder<int>(System.TimeProvider.System, invalidAggregationType, instrument);

        var ex = Assert.Throws<InvalidOperationException>(() => metricValuesHolder.ReceiveValue(50, new KeyValuePair<string, object?>[0].AsSpan()));
        Assert.Equal($"Aggregation type {invalidAggregationType} is not supported.", ex.Message);
    }
}
