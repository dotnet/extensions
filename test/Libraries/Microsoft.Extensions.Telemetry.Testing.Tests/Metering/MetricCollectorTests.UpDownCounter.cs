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
    public void UpDownCounter_BasicTest()
    {
        using var metricCollector = new MetricCollector();
        using var meter = new Meter(string.Empty);

        UpDownCounterBasicTest<byte>(meter, metricCollector, 2, 5, 10, 17);
        UpDownCounterBasicTest<short>(meter, metricCollector, 20, 50, 100, 170);
        UpDownCounterBasicTest<int>(meter, metricCollector, 200, 500, 1000, 1700);
        UpDownCounterBasicTest<long>(meter, metricCollector, 2000L, 5000L, 10000L, 17000L);
        UpDownCounterBasicTest<float>(meter, metricCollector, 1.22f, 3.44f, 0, 1.22f + 3.44f);
        UpDownCounterBasicTest<double>(meter, metricCollector, 5.22, 6.44, 10, 5.22 + 6.44 + 10);
        UpDownCounterBasicTest<decimal>(meter, metricCollector, 0.99m, 15, 25.99m, 41.98m);
    }

    [Fact]
    public void UpDownCounter_StringDimensionsAreHandled()
    {
        using var metricCollector = new MetricCollector();
        using var meter = new Meter(string.Empty);

        UpDownCounterWithStringDimensionsTest(meter, metricCollector, byte.MinValue);
        UpDownCounterWithStringDimensionsTest(meter, metricCollector, short.MaxValue);
        UpDownCounterWithStringDimensionsTest(meter, metricCollector, int.MaxValue);
        UpDownCounterWithStringDimensionsTest(meter, metricCollector, long.MaxValue);
        UpDownCounterWithStringDimensionsTest(meter, metricCollector, float.MaxValue);
        UpDownCounterWithStringDimensionsTest(meter, metricCollector, double.MaxValue);
        UpDownCounterWithStringDimensionsTest(meter, metricCollector, decimal.MaxValue);
    }

    [Fact]
    public void UpDownCounter_NumericDimensionsAreHandled()
    {
        using var metricCollector = new MetricCollector();
        using var meter = new Meter(string.Empty);

        UpDownCounterWithNumericDimensionsTest(meter, metricCollector, byte.MinValue);
        UpDownCounterWithNumericDimensionsTest(meter, metricCollector, short.MaxValue);
        UpDownCounterWithNumericDimensionsTest(meter, metricCollector, int.MaxValue);
        UpDownCounterWithNumericDimensionsTest(meter, metricCollector, long.MaxValue);
        UpDownCounterWithNumericDimensionsTest(meter, metricCollector, float.MaxValue);
        UpDownCounterWithNumericDimensionsTest(meter, metricCollector, double.MaxValue);
        UpDownCounterWithNumericDimensionsTest(meter, metricCollector, decimal.MaxValue);
    }

    [Fact]
    public void UpDownCounter_ArrayDimensionsAreHandled()
    {
        using var metricCollector = new MetricCollector();
        using var meter = new Meter(string.Empty);

        UpDownCounterWithArrayDimensionsTest(meter, metricCollector, byte.MinValue);
        UpDownCounterWithArrayDimensionsTest(meter, metricCollector, short.MaxValue);
        UpDownCounterWithArrayDimensionsTest(meter, metricCollector, int.MaxValue);
        UpDownCounterWithArrayDimensionsTest(meter, metricCollector, long.MaxValue);
        UpDownCounterWithArrayDimensionsTest(meter, metricCollector, float.MaxValue);
        UpDownCounterWithArrayDimensionsTest(meter, metricCollector, double.MaxValue);
        UpDownCounterWithArrayDimensionsTest(meter, metricCollector, decimal.MaxValue);
    }

    private static void UpDownCounterBasicTest<T>(Meter meter, MetricCollector metricCollector, T value, T valueToAdd, T valueToAdd1, T totalSum)
        where T : struct
    {
        var upDownCounter1 = meter.CreateUpDownCounter<T>(Guid.NewGuid().ToString());
        var holder1 = metricCollector.GetUpDownCounterValues<T>(upDownCounter1.Name);

        Assert.NotNull(holder1);

        upDownCounter1.Add(value);

        var recordedValue1 = metricCollector.GetUpDownCounterValue<T>(upDownCounter1.Name);

        Assert.NotNull(recordedValue1);
        Assert.Equal(value, recordedValue1.Value);

        upDownCounter1.Add(valueToAdd);
        upDownCounter1.Add(valueToAdd1);

        var recordedValue2 = metricCollector.GetUpDownCounterValue<T>(upDownCounter1.Name);

        Assert.Equal(totalSum, recordedValue2!.Value);

        var upDownCounter2 = meter.CreateUpDownCounter<T>(Guid.NewGuid().ToString());
        var holder2 = metricCollector.GetUpDownCounterValues<T>(upDownCounter2.Name);

        Assert.NotNull(holder2);

        upDownCounter2.Add(value);

        Assert.Equal(value, metricCollector.GetUpDownCounterValue<T>(upDownCounter2.Name)!.Value);

        upDownCounter2.Add(valueToAdd);
        upDownCounter2.Add(valueToAdd1);

        Assert.Equal(totalSum, metricCollector.GetUpDownCounterValue<T>(upDownCounter2.Name)!.Value);
    }

    private static void UpDownCounterWithStringDimensionsTest<T>(Meter meter, MetricCollector metricCollector, T value)
        where T : struct
    {
        var upDownCounter = meter.CreateUpDownCounter<T>(Guid.NewGuid().ToString());

        // No dimensions
        upDownCounter.Add(value);

        Assert.Equal(value, metricCollector.GetUpDownCounterValue<T>(upDownCounter.Name)!.Value);

        // One dimension
        var dimension1 = Guid.NewGuid().ToString();
        var dimension1Val = Guid.NewGuid().ToString();
        upDownCounter.Add(value, new KeyValuePair<string, object?>(dimension1, dimension1Val));

        Assert.Equal(value, metricCollector.GetUpDownCounterValue<T>(upDownCounter.Name, new KeyValuePair<string, object?>(dimension1, dimension1Val))!.Value);

        // Two dimensions
        var dimension2 = Guid.NewGuid().ToString();
        var dimension2Val = Guid.NewGuid().ToString();
        upDownCounter.Add(value, new KeyValuePair<string, object?>(dimension1, dimension1Val), new KeyValuePair<string, object?>(dimension2, dimension2Val));

        var actualValue = metricCollector.GetUpDownCounterValue<T>(
            upDownCounter.Name,
            new KeyValuePair<string, object?>(dimension2, dimension2Val),
            new KeyValuePair<string, object?>(dimension1, dimension1Val));
        Assert.Equal(value, actualValue!.Value);
    }

    private static void UpDownCounterWithNumericDimensionsTest<T>(Meter meter, MetricCollector metricCollector, T value)
        where T : struct
    {
        var upDownCounter = meter.CreateUpDownCounter<T>(Guid.NewGuid().ToString());

        // One dimension
        var intDimension = Guid.NewGuid().ToString();
        int intVal = 15555;
        upDownCounter.Add(value, new KeyValuePair<string, object?>(intDimension, intVal));

        Assert.Equal(value, metricCollector.GetUpDownCounterValue<T>(upDownCounter.Name, new KeyValuePair<string, object?>(intDimension, intVal))!.Value);

        // Two dimensions
        var doubleDimension = Guid.NewGuid().ToString();
        double doubleVal = 1111.9999d;
        upDownCounter.Add(value, new KeyValuePair<string, object?>(intDimension, intVal), new KeyValuePair<string, object?>(doubleDimension, doubleVal));

        var actualValue = metricCollector.GetUpDownCounterValue<T>(
            upDownCounter.Name,
            new KeyValuePair<string, object?>(intDimension, intVal),
            new KeyValuePair<string, object?>(doubleDimension, doubleVal));
        Assert.Equal(value, actualValue!.Value);

        // Three dimensions
        var longDimension = Guid.NewGuid().ToString();
        long longVal = 1_999_988_887_777_111L;

        upDownCounter.Add(value, new KeyValuePair<string, object?>(intDimension, intVal), new KeyValuePair<string, object?>(longDimension, longVal),
            new KeyValuePair<string, object?>(doubleDimension, doubleVal));

        actualValue = metricCollector.GetUpDownCounterValue<T>(upDownCounter.Name,
                        new KeyValuePair<string, object?>(longDimension, longVal),
                        new KeyValuePair<string, object?>(intDimension, intVal),
                        new KeyValuePair<string, object?>(doubleDimension, doubleVal));
        Assert.Equal(value, actualValue!.Value);
    }

    private static void UpDownCounterWithArrayDimensionsTest<T>(Meter meter, MetricCollector metricCollector, T value)
        where T : struct
    {
        var upDownCounter = meter.CreateUpDownCounter<T>(Guid.NewGuid().ToString());

        // One dimension
        var intArrayDimension = Guid.NewGuid().ToString();
        int[] intArrVal = new[] { 12, 55, 2023 };
        upDownCounter.Add(value, new KeyValuePair<string, object?>(intArrayDimension, intArrVal));

        Assert.Equal(value, metricCollector.GetUpDownCounterValue<T>(upDownCounter.Name, new KeyValuePair<string, object?>(intArrayDimension, intArrVal))!.Value);

        // Two dimensions
        var doubleArrayDimension = Guid.NewGuid().ToString();
        double[] doubleArrVal = new[] { 1111.9999d, 0, 3.1415 };
        upDownCounter.Add(value, new KeyValuePair<string, object?>(intArrayDimension, intArrVal), new KeyValuePair<string, object?>(doubleArrayDimension, doubleArrVal));

        var actualValue = metricCollector.GetUpDownCounterValue<T>(upDownCounter.Name,
            new KeyValuePair<string, object?>(intArrayDimension, intArrVal),
            new KeyValuePair<string, object?>(doubleArrayDimension, doubleArrVal));

        Assert.Equal(value, actualValue!.Value);

        // Three dimensions
        var longArrayDimension = Guid.NewGuid().ToString();
        long[] longArrVal = new[] { 1_999_988_887_777_111L, 1_111_222_333_444_555L, 1_999_988_887_777_111L, 0 };

        upDownCounter.Add(value, new KeyValuePair<string, object?>(intArrayDimension, intArrVal),
            new KeyValuePair<string, object?>(doubleArrayDimension, doubleArrVal),
            new KeyValuePair<string, object?>(longArrayDimension, longArrVal));

        actualValue = metricCollector.GetUpDownCounterValue<T>(upDownCounter.Name,
            new KeyValuePair<string, object?>(longArrayDimension, longArrVal),
            new KeyValuePair<string, object?>(intArrayDimension, intArrVal),
            new KeyValuePair<string, object?>(doubleArrayDimension, doubleArrVal));

        Assert.Equal(value, actualValue!.Value);
    }
}
