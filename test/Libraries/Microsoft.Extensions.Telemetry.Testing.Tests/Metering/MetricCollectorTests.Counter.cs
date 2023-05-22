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
    public void Counter_BasicTest()
    {
        using var metricCollector = new MetricCollector();
        using var meter = new Meter(string.Empty);

        CounterBasicTest<byte>(meter, metricCollector, 2, 5, 10, 17);
        CounterBasicTest<short>(meter, metricCollector, 20, 50, 100, 170);
        CounterBasicTest<int>(meter, metricCollector, 200, 500, 1000, 1700);
        CounterBasicTest<long>(meter, metricCollector, 2000L, 5000L, 10000L, 17000L);
        CounterBasicTest<float>(meter, metricCollector, 1.22f, 3.44f, 0, 1.22f + 3.44f);
        CounterBasicTest<double>(meter, metricCollector, 5.22, 6.44, 10, 5.22 + 6.44 + 10);
        CounterBasicTest<decimal>(meter, metricCollector, 0.99m, 15, 25.99m, 41.98m);
    }

    [Fact]
    public void Counter_StringDimensionsAreHandled()
    {
        using var metricCollector = new MetricCollector();
        using var meter = new Meter(string.Empty);

        CounterWithStringDimensionsTest(meter, metricCollector, byte.MinValue);
        CounterWithStringDimensionsTest(meter, metricCollector, short.MaxValue);
        CounterWithStringDimensionsTest(meter, metricCollector, int.MaxValue);
        CounterWithStringDimensionsTest(meter, metricCollector, long.MaxValue);
        CounterWithStringDimensionsTest(meter, metricCollector, float.MaxValue);
        CounterWithStringDimensionsTest(meter, metricCollector, double.MaxValue);
        CounterWithStringDimensionsTest(meter, metricCollector, decimal.MaxValue);
    }

    [Fact]
    public void Counter_NumericDimensionsAreHandled()
    {
        using var metricCollector = new MetricCollector();
        using var meter = new Meter(string.Empty);

        CounterWithNumericDimensionsTest(meter, metricCollector, byte.MinValue);
        CounterWithNumericDimensionsTest(meter, metricCollector, short.MaxValue);
        CounterWithNumericDimensionsTest(meter, metricCollector, int.MaxValue);
        CounterWithNumericDimensionsTest(meter, metricCollector, long.MaxValue);
        CounterWithNumericDimensionsTest(meter, metricCollector, float.MaxValue);
        CounterWithNumericDimensionsTest(meter, metricCollector, double.MaxValue);
        CounterWithNumericDimensionsTest(meter, metricCollector, decimal.MaxValue);
    }

    [Fact]
    public void Counter_ArrayDimensionsAreHandled()
    {
        using var metricCollector = new MetricCollector();
        using var meter = new Meter(string.Empty);

        CounterWithArrayDimensionsTest(meter, metricCollector, byte.MinValue);
        CounterWithArrayDimensionsTest(meter, metricCollector, short.MaxValue);
        CounterWithArrayDimensionsTest(meter, metricCollector, int.MaxValue);
        CounterWithArrayDimensionsTest(meter, metricCollector, long.MaxValue);
        CounterWithArrayDimensionsTest(meter, metricCollector, float.MaxValue);
        CounterWithArrayDimensionsTest(meter, metricCollector, double.MaxValue);
        CounterWithArrayDimensionsTest(meter, metricCollector, decimal.MaxValue);
    }

    private static void CounterBasicTest<T>(Meter meter, MetricCollector metricCollector, T value, T valueToAdd, T valueToAdd1, T totalSum)
        where T : struct
    {
        var counter1 = meter.CreateCounter<T>(Guid.NewGuid().ToString());
        var holder1 = metricCollector.GetCounterValues<T>(counter1.Name);

        Assert.NotNull(holder1);

        counter1.Add(value);

        var recordedValue1 = metricCollector.GetCounterValue<T>(counter1.Name);

        Assert.NotNull(recordedValue1);
        Assert.Equal(value, recordedValue1.Value);

        counter1.Add(valueToAdd);
        counter1.Add(valueToAdd1);

        var recordedValue2 = metricCollector.GetCounterValue<T>(counter1.Name);

        Assert.Equal(totalSum, recordedValue2!.Value);

        var counter2 = meter.CreateCounter<T>(Guid.NewGuid().ToString());
        var holder2 = metricCollector.GetCounterValues<T>(counter2.Name);

        Assert.NotNull(holder2);

        counter2.Add(value);

        Assert.Equal(value, metricCollector.GetCounterValue<T>(counter2.Name)!.Value);

        counter2.Add(valueToAdd);
        counter2.Add(valueToAdd1);

        Assert.Equal(totalSum, metricCollector.GetCounterValue<T>(counter2.Name)!.Value);
    }

    private static void CounterWithStringDimensionsTest<T>(Meter meter, MetricCollector metricCollector, T value)
        where T : struct
    {
        var counter = meter.CreateCounter<T>(Guid.NewGuid().ToString());

        // No dimensions
        counter.Add(value);

        Assert.Equal(value, metricCollector.GetCounterValue<T>(counter.Name)!.Value);

        // One dimension
        var dimension1 = Guid.NewGuid().ToString();
        var dimension1Val = Guid.NewGuid().ToString();
        counter.Add(value, new KeyValuePair<string, object?>(dimension1, dimension1Val));

        Assert.Equal(value, metricCollector.GetCounterValue<T>(counter.Name, new KeyValuePair<string, object?>(dimension1, dimension1Val))!.Value);

        // Two dimensions
        var dimension2 = Guid.NewGuid().ToString();
        var dimension2Val = Guid.NewGuid().ToString();
        counter.Add(value, new KeyValuePair<string, object?>(dimension1, dimension1Val), new KeyValuePair<string, object?>(dimension2, dimension2Val));

        Assert.Equal(value,
            metricCollector.GetCounterValue<T>(counter.Name, new KeyValuePair<string, object?>(dimension2, dimension2Val), new KeyValuePair<string, object?>(dimension1, dimension1Val))!.Value);
    }

    private static void CounterWithNumericDimensionsTest<T>(Meter meter, MetricCollector metricCollector, T value)
        where T : struct
    {
        var counter = meter.CreateCounter<T>(Guid.NewGuid().ToString());

        // One dimension
        var intDimension = Guid.NewGuid().ToString();
        const int IntVal = 15555;
        counter.Add(value, new KeyValuePair<string, object?>(intDimension, IntVal));

        Assert.Equal(value, metricCollector.GetCounterValue<T>(counter.Name, new KeyValuePair<string, object?>(intDimension, IntVal))!.Value);

        // Two dimensions
        var doubleDimension = Guid.NewGuid().ToString();
        const double DoubleVal = 1111.9999d;
        counter.Add(value, new KeyValuePair<string, object?>(intDimension, IntVal), new KeyValuePair<string, object?>(doubleDimension, DoubleVal));

        Assert.Equal(value,
            metricCollector.GetCounterValue<T>(counter.Name, new KeyValuePair<string, object?>(intDimension, IntVal), new KeyValuePair<string, object?>(doubleDimension, DoubleVal))!.Value);

        // Three dimensions
        var longDimension = Guid.NewGuid().ToString();
        const long LongVal = 1_999_988_887_777_111L;

        counter.Add(value, new KeyValuePair<string, object?>(intDimension, IntVal), new KeyValuePair<string, object?>(longDimension, LongVal),
            new KeyValuePair<string, object?>(doubleDimension, DoubleVal));

        var actualValue = metricCollector.GetCounterValue<T>(counter.Name,
                        new KeyValuePair<string, object?>(longDimension, LongVal),
                        new KeyValuePair<string, object?>(intDimension, IntVal),
                        new KeyValuePair<string, object?>(doubleDimension, DoubleVal));

        Assert.Equal(value, actualValue!.Value);
    }

    private static void CounterWithArrayDimensionsTest<T>(Meter meter, MetricCollector metricCollector, T value)
        where T : struct
    {
        var counter = meter.CreateCounter<T>(Guid.NewGuid().ToString());

        // One dimension
        var intArrayDimension = Guid.NewGuid().ToString();
        int[] intArrVal = new[] { 12, 55, 2023 };
        counter.Add(value, new KeyValuePair<string, object?>(intArrayDimension, intArrVal));

        Assert.Equal(value, metricCollector.GetCounterValue<T>(counter.Name, new KeyValuePair<string, object?>(intArrayDimension, intArrVal))!.Value);

        // Two dimensions
        var doubleArrayDimension = Guid.NewGuid().ToString();
        double[] doubleArrVal = new[] { 1111.9999d, 0, 3.1415 };
        counter.Add(value, new KeyValuePair<string, object?>(intArrayDimension, intArrVal), new KeyValuePair<string, object?>(doubleArrayDimension, doubleArrVal));

        var actualValue = metricCollector.GetCounterValue<T>(counter.Name,
            new KeyValuePair<string, object?>(intArrayDimension, intArrVal),
            new KeyValuePair<string, object?>(doubleArrayDimension, doubleArrVal));

        Assert.Equal(value, actualValue!.Value);

        // Three dimensions
        var longArrayDimension = Guid.NewGuid().ToString();
        long[] longArrVal = new[] { 1_999_988_887_777_111L, 1_111_222_333_444_555L, 1_999_988_887_777_111L, 0 };

        counter.Add(value, new KeyValuePair<string, object?>(intArrayDimension, intArrVal),
            new KeyValuePair<string, object?>(doubleArrayDimension, doubleArrVal),
            new KeyValuePair<string, object?>(longArrayDimension, longArrVal));

        actualValue = metricCollector.GetCounterValue<T>(counter.Name,
            new KeyValuePair<string, object?>(longArrayDimension, longArrVal),
            new KeyValuePair<string, object?>(intArrayDimension, intArrVal),
            new KeyValuePair<string, object?>(doubleArrayDimension, doubleArrVal));

        Assert.Equal(value, actualValue!.Value);
    }
}
