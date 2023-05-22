// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Telemetry.Testing.Metering;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Testing.Metering.Test;

public partial class MetricCollectorTests
{
    [Fact]
    public void Histogram_BasicTest()
    {
        using var metricCollector = new MetricCollector();
        using var meter = new Meter(string.Empty);

        HistogramBasicTest<byte>(meter, metricCollector, 2, 5);
        HistogramBasicTest<short>(meter, metricCollector, 20, 50);
        HistogramBasicTest<int>(meter, metricCollector, 200, 500);
        HistogramBasicTest<long>(meter, metricCollector, 2000L, 5000L);
        HistogramBasicTest<float>(meter, metricCollector, 1.22f, 3.44f);
        HistogramBasicTest<double>(meter, metricCollector, 5.22, 6.44);
        HistogramBasicTest<decimal>(meter, metricCollector, 0.99m, 15);
    }

    [Fact]
    public void Histogram_StringDimensionsAreHandled()
    {
        using var metricCollector = new MetricCollector();
        using var meter = new Meter(string.Empty);

        HistogramWithStringDimensionsTest(meter, metricCollector, byte.MinValue);
        HistogramWithStringDimensionsTest(meter, metricCollector, short.MinValue);
        HistogramWithStringDimensionsTest(meter, metricCollector, int.MinValue);
        HistogramWithStringDimensionsTest(meter, metricCollector, long.MinValue);
        HistogramWithStringDimensionsTest(meter, metricCollector, float.MinValue);
        HistogramWithStringDimensionsTest(meter, metricCollector, double.MinValue);
        HistogramWithStringDimensionsTest(meter, metricCollector, decimal.MinValue);
    }

    [Fact]
    public void Histogram_NumericDimensionsAreHandled()
    {
        using var metricCollector = new MetricCollector();
        using var meter = new Meter(string.Empty);

        HistogramWithNumericDimensionsTest(meter, metricCollector, byte.MinValue);
        HistogramWithNumericDimensionsTest(meter, metricCollector, short.MaxValue);
        HistogramWithNumericDimensionsTest(meter, metricCollector, int.MaxValue);
        HistogramWithNumericDimensionsTest(meter, metricCollector, long.MaxValue);
        HistogramWithNumericDimensionsTest(meter, metricCollector, float.MaxValue);
        HistogramWithNumericDimensionsTest(meter, metricCollector, double.MaxValue);
        HistogramWithNumericDimensionsTest(meter, metricCollector, decimal.MaxValue);
    }

    [Fact]
    public void Histogram_ArrayDimensionsAreHandled()
    {
        using var metricCollector = new MetricCollector();
        using var meter = new Meter(string.Empty);

        HistogramWithArrayDimensionsTest(meter, metricCollector, byte.MinValue);
        HistogramWithArrayDimensionsTest(meter, metricCollector, short.MaxValue);
        HistogramWithArrayDimensionsTest(meter, metricCollector, int.MaxValue);
        HistogramWithArrayDimensionsTest(meter, metricCollector, long.MaxValue);
        HistogramWithArrayDimensionsTest(meter, metricCollector, float.MaxValue);
        HistogramWithArrayDimensionsTest(meter, metricCollector, double.MaxValue);
        HistogramWithArrayDimensionsTest(meter, metricCollector, decimal.MaxValue);
    }

    private static void HistogramBasicTest<T>(Meter meter, MetricCollector metricCollector, T value, T secondValue)
        where T : struct
    {
        var histogram = meter.CreateHistogram<T>(Guid.NewGuid().ToString());
        var holder1 = metricCollector.GetHistogramValues<T>(histogram.Name);

        Assert.NotNull(holder1);

        histogram.Record(value);

        var recordedValue1 = metricCollector.GetHistogramValue<T>(histogram.Name);

        Assert.NotNull(recordedValue1);
        Assert.Equal(value, recordedValue1.Value);
        Assert.Equal(1, holder1.AllValues.Count);

        histogram.Record(secondValue);

        var recordedValue2 = metricCollector.GetHistogramValue<T>(histogram.Name);

        Assert.Equal(secondValue, recordedValue2!.Value);
        Assert.Equal(2, holder1.AllValues.Count);

        var histogram2 = meter.CreateHistogram<T>(Guid.NewGuid().ToString());
        var holder2 = metricCollector.GetHistogramValues<T>(histogram2.Name);

        Assert.NotNull(holder2);

        histogram2.Record(value);

        Assert.Equal(value, metricCollector.GetHistogramValue<T>(histogram2.Name)!.Value);

        histogram2.Record(secondValue);

        Assert.Equal(secondValue, metricCollector.GetHistogramValue<T>(histogram2.Name)!.Value);
    }

    private static void HistogramWithStringDimensionsTest<T>(Meter meter, MetricCollector metricCollector, T value)
        where T : struct
    {
        var histogram = meter.CreateHistogram<T>(Guid.NewGuid().ToString());

        // No dimensions
        histogram.Record(value);

        Assert.Equal(value, metricCollector.GetHistogramValue<T>(histogram.Name)!.Value);

        // One dimension
        var dimension1 = Guid.NewGuid().ToString();
        var dimension1Val = Guid.NewGuid().ToString();
        histogram.Record(value, new KeyValuePair<string, object?>(dimension1, dimension1Val));

        Assert.Equal(value, metricCollector.GetHistogramValue<T>(histogram.Name, new KeyValuePair<string, object?>(dimension1, dimension1Val))!.Value);

        // Two dimensions
        var dimension2 = Guid.NewGuid().ToString();
        var dimension2Val = Guid.NewGuid().ToString();
        histogram.Record(value, new KeyValuePair<string, object?>(dimension1, dimension1Val), new KeyValuePair<string, object?>(dimension2, dimension2Val));

        Assert.Equal(value,
            metricCollector.GetHistogramValue<T>(histogram.Name, new KeyValuePair<string, object?>(dimension2, dimension2Val), new KeyValuePair<string, object?>(dimension1, dimension1Val))!.Value);
    }

    private static void HistogramWithNumericDimensionsTest<T>(Meter meter, MetricCollector metricCollector, T value)
        where T : struct
    {
        var histogram = meter.CreateHistogram<T>(Guid.NewGuid().ToString());

        // One dimension
        var intDimension = Guid.NewGuid().ToString();
        int intVal = 15555;
        histogram.Record(value, new KeyValuePair<string, object?>(intDimension, intVal));

        Assert.Equal(value, metricCollector.GetHistogramValue<T>(histogram.Name, new KeyValuePair<string, object?>(intDimension, intVal))!.Value);

        // Two dimensions
        var doubleDimension = Guid.NewGuid().ToString();
        double doubleVal = 1111.9999d;
        histogram.Record(value, new KeyValuePair<string, object?>(intDimension, intVal), new KeyValuePair<string, object?>(doubleDimension, doubleVal));

        Assert.Equal(value,
            metricCollector.GetHistogramValue<T>(histogram.Name, new KeyValuePair<string, object?>(intDimension, intVal), new KeyValuePair<string, object?>(doubleDimension, doubleVal))!.Value);

        // Three dimensions
        var longDimension = Guid.NewGuid().ToString();
        long longVal = 1_999_988_887_777_111L;

        histogram.Record(value, new KeyValuePair<string, object?>(intDimension, intVal), new KeyValuePair<string, object?>(longDimension, longVal),
            new KeyValuePair<string, object?>(doubleDimension, doubleVal));

        var actualValue = metricCollector.GetHistogramValue<T>(histogram.Name,
                        new KeyValuePair<string, object?>(longDimension, longVal),
                        new KeyValuePair<string, object?>(intDimension, intVal),
                        new KeyValuePair<string, object?>(doubleDimension, doubleVal));
        Assert.Equal(value, actualValue!.Value);
    }

    private static void HistogramWithArrayDimensionsTest<T>(Meter meter, MetricCollector metricCollector, T value)
        where T : struct
    {
        var histogram = meter.CreateHistogram<T>(Guid.NewGuid().ToString());

        // One dimension
        var intArrayDimension = Guid.NewGuid().ToString();
        int[] intArrVal = new[] { 12, 55, 2023 };
        histogram.Record(value, new KeyValuePair<string, object?>(intArrayDimension, intArrVal));

        Assert.Equal(value, metricCollector.GetHistogramValue<T>(histogram.Name, new KeyValuePair<string, object?>(intArrayDimension, intArrVal))!.Value);

        // Two dimensions
        var doubleArrayDimension = Guid.NewGuid().ToString();
        double[] doubleArrVal = new[] { 1111.9999d, 0, 3.1415 };
        histogram.Record(value, new KeyValuePair<string, object?>(intArrayDimension, intArrVal), new KeyValuePair<string, object?>(doubleArrayDimension, doubleArrVal));

        var actualValue = metricCollector.GetHistogramValue<T>(histogram.Name,
            new KeyValuePair<string, object?>(intArrayDimension, intArrVal),
            new KeyValuePair<string, object?>(doubleArrayDimension, doubleArrVal));

        Assert.Equal(value, actualValue!.Value);

        // Three dimensions
        var longArrayDimension = Guid.NewGuid().ToString();
        long[] longArrVal = new[] { 1_999_988_887_777_111L, 1_111_222_333_444_555L, 1_999_988_887_777_111L, 0 };

        histogram.Record(value, new KeyValuePair<string, object?>(intArrayDimension, intArrVal),
            new KeyValuePair<string, object?>(doubleArrayDimension, doubleArrVal),
            new KeyValuePair<string, object?>(longArrayDimension, longArrVal));

        actualValue = metricCollector.GetHistogramValue<T>(histogram.Name,
            new KeyValuePair<string, object?>(longArrayDimension, longArrVal),
            new KeyValuePair<string, object?>(intArrayDimension, intArrVal),
            new KeyValuePair<string, object?>(doubleArrayDimension, doubleArrVal));

        Assert.Equal(value, actualValue!.Value);
    }
}
