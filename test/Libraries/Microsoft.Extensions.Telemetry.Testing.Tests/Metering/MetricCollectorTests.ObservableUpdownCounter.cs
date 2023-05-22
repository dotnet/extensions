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
    public void ObservableUpDownCounter_BasicTest()
    {
        using var metricCollector = new MetricCollector();
        using var meter = new Meter(string.Empty);

        ObservableUpDownCounterBasicTest<byte>(meter, metricCollector, 2, 5, 10, 17);
        ObservableUpDownCounterBasicTest<short>(meter, metricCollector, 20, 50, 100, 170);
        ObservableUpDownCounterBasicTest<int>(meter, metricCollector, 200, 500, 1000, 1700);
        ObservableUpDownCounterBasicTest<long>(meter, metricCollector, 2000L, 5000L, 10000L, 17000L);
        ObservableUpDownCounterBasicTest<float>(meter, metricCollector, 1.22f, 3.44f, 0, 1.22f + 3.44f);
        ObservableUpDownCounterBasicTest<double>(meter, metricCollector, 5.22, 6.44, 10, 5.22 + 6.44 + 10);
        ObservableUpDownCounterBasicTest<decimal>(meter, metricCollector, 0.99m, 15, 25.99m, 41.98m);
    }

    [Fact]
    public void ObservableUpDownCounter_StringDimensionsAreHandled()
    {
        using var metricCollector = new MetricCollector();
        using var meter = new Meter(string.Empty);

        ObservableUpDownCounterWithStringDimensionsTest(meter, metricCollector, byte.MinValue);
        ObservableUpDownCounterWithStringDimensionsTest(meter, metricCollector, short.MaxValue);
        ObservableUpDownCounterWithStringDimensionsTest(meter, metricCollector, int.MaxValue);
        ObservableUpDownCounterWithStringDimensionsTest(meter, metricCollector, long.MaxValue);
        ObservableUpDownCounterWithStringDimensionsTest(meter, metricCollector, float.MaxValue);
        ObservableUpDownCounterWithStringDimensionsTest(meter, metricCollector, double.MaxValue);
        ObservableUpDownCounterWithStringDimensionsTest(meter, metricCollector, decimal.MaxValue);
    }

    [Fact]
    public void ObservableUpDownCounter_NumericDimensionsAreHandled()
    {
        using var metricCollector = new MetricCollector();
        using var meter = new Meter(string.Empty);

        ObservableUpDownCounterWithNumericDimensionsTest(meter, metricCollector, byte.MinValue);
        ObservableUpDownCounterWithNumericDimensionsTest(meter, metricCollector, short.MaxValue);
        ObservableUpDownCounterWithNumericDimensionsTest(meter, metricCollector, int.MaxValue);
        ObservableUpDownCounterWithNumericDimensionsTest(meter, metricCollector, long.MaxValue);
        ObservableUpDownCounterWithNumericDimensionsTest(meter, metricCollector, float.MaxValue);
        ObservableUpDownCounterWithNumericDimensionsTest(meter, metricCollector, double.MaxValue);
        ObservableUpDownCounterWithNumericDimensionsTest(meter, metricCollector, decimal.MaxValue);
    }

    [Fact]
    public void ObservableUpDownCounter_ArrayDimensionsAreHandled()
    {
        using var metricCollector = new MetricCollector();
        using var meter = new Meter(string.Empty);

        ObservableUpDownCounterWithArrayDimensionsTest(meter, metricCollector, byte.MinValue);
        ObservableUpDownCounterWithArrayDimensionsTest(meter, metricCollector, short.MaxValue);
        ObservableUpDownCounterWithArrayDimensionsTest(meter, metricCollector, int.MaxValue);
        ObservableUpDownCounterWithArrayDimensionsTest(meter, metricCollector, long.MaxValue);
        ObservableUpDownCounterWithArrayDimensionsTest(meter, metricCollector, float.MaxValue);
        ObservableUpDownCounterWithArrayDimensionsTest(meter, metricCollector, double.MaxValue);
        ObservableUpDownCounterWithArrayDimensionsTest(meter, metricCollector, decimal.MaxValue);
    }

    private static void ObservableUpDownCounterBasicTest<T>(Meter meter, MetricCollector metricCollector, T value1, T value2, T value3, T value4)
        where T : struct
    {
        var states = new[] { value1, value2, value3, value4 };

        int index = 0;
        var observableFunc = () =>
        {
            if (index >= states.Length)
            {
                index = 0;
            }

            return states[index++];
        };

        var observableUpDownCounter1 = meter.CreateObservableUpDownCounter<T>(Guid.NewGuid().ToString(), observableFunc);
        var holder1 = metricCollector.GetObservableUpDownCounterValues<T>(observableUpDownCounter1.Name);

        Assert.NotNull(holder1);

        metricCollector.CollectObservableInstruments();
        var recordedValue1 = metricCollector.GetObservableUpDownCounterValue<T>(observableUpDownCounter1.Name);

        Assert.NotNull(recordedValue1);
        Assert.Equal(value1, recordedValue1.Value);

        metricCollector.CollectObservableInstruments();

        Assert.Equal(value2, metricCollector.GetObservableUpDownCounterValue<T>(observableUpDownCounter1.Name)!.Value);

        metricCollector.CollectObservableInstruments();

        Assert.Equal(value3, metricCollector.GetObservableUpDownCounterValue<T>(observableUpDownCounter1.Name)!.Value);

        metricCollector.CollectObservableInstruments();

        Assert.Equal(value4, metricCollector.GetObservableUpDownCounterValue<T>(observableUpDownCounter1.Name)!.Value);

        int index2 = 0;
        var observableFunc2 = () =>
        {
            if (index2 >= states.Length)
            {
                index2 = 0;
            }

            return states[index2++];
        };

        var observableUpDownCounter2 = meter.CreateObservableUpDownCounter<T>(Guid.NewGuid().ToString(), observableFunc2);
        var holder2 = metricCollector.GetObservableUpDownCounterValues<T>(observableUpDownCounter2.Name);

        Assert.NotNull(holder2);

        metricCollector.CollectObservableInstruments();

        Assert.Equal(value1, metricCollector.GetObservableUpDownCounterValue<T>(observableUpDownCounter2.Name)!.Value);

        metricCollector.CollectObservableInstruments();

        Assert.Equal(value2, metricCollector.GetObservableUpDownCounterValue<T>(observableUpDownCounter2.Name)!.Value);

        metricCollector.CollectObservableInstruments();

        Assert.Equal(value3, metricCollector.GetObservableUpDownCounterValue<T>(observableUpDownCounter2.Name)!.Value);

        metricCollector.CollectObservableInstruments();

        Assert.Equal(value4, metricCollector.GetObservableUpDownCounterValue<T>(observableUpDownCounter2.Name)!.Value);
    }

    private static void ObservableUpDownCounterWithStringDimensionsTest<T>(Meter meter, MetricCollector metricCollector, T value)
        where T : struct
    {
        var dimension1 = Guid.NewGuid().ToString();
        var dimension1Val = Guid.NewGuid().ToString();
        var dimension2 = Guid.NewGuid().ToString();
        var dimension2Val = Guid.NewGuid().ToString();

        var measurements = new[]
        {
            new Measurement<T>(value),
            new Measurement<T>(value, new KeyValuePair<string, object?>(dimension1, dimension1Val)),
            new Measurement<T>(value, new KeyValuePair<string, object?>(dimension1, dimension1Val), new KeyValuePair<string, object?>(dimension2, dimension2Val)),
        };

        int index = 0;
        var observableFunc = () =>
        {
            if (index >= measurements.Length)
            {
                index = 0;
            }

            return measurements[index++];
        };

        var observableUpDownCounter = meter.CreateObservableUpDownCounter<T>(Guid.NewGuid().ToString(), observableFunc);

        // No dimensions
        metricCollector.CollectObservableInstruments();
        Assert.Equal(value, metricCollector.GetObservableUpDownCounterValue<T>(observableUpDownCounter.Name)!.Value);

        // One dimension
        metricCollector.CollectObservableInstruments();
        Assert.Equal(value, metricCollector.GetObservableUpDownCounterValue<T>(observableUpDownCounter.Name, new KeyValuePair<string, object?>(dimension1, dimension1Val))!.Value);

        // Two dimensions
        metricCollector.CollectObservableInstruments();
        var actualValue = metricCollector.GetObservableUpDownCounterValue<T>(
            observableUpDownCounter.Name,
            new KeyValuePair<string, object?>(dimension2, dimension2Val),
            new KeyValuePair<string, object?>(dimension1, dimension1Val));
        Assert.Equal(value, actualValue!.Value);
    }

    private static void ObservableUpDownCounterWithNumericDimensionsTest<T>(Meter meter, MetricCollector metricCollector, T value)
        where T : struct
    {
        var intDimension = Guid.NewGuid().ToString();
        int intVal = 15555;
        var doubleDimension = Guid.NewGuid().ToString();
        double doubleVal = 1111.9999d;
        var longDimension = Guid.NewGuid().ToString();
        long longVal = 1_999_988_887_777_111L;

        var measurements = new[]
        {
            new Measurement<T>(value, new KeyValuePair<string, object?>(intDimension, intVal)),
            new Measurement<T>(value, new KeyValuePair<string, object?>(intDimension, intVal), new KeyValuePair<string, object?>(doubleDimension, doubleVal)),
            new Measurement<T>(value, new KeyValuePair<string, object?>(intDimension, intVal),
                new KeyValuePair<string, object?>(doubleDimension, doubleVal),
                new KeyValuePair<string, object?>(longDimension, longVal))
        };

        int index = 0;
        var observableFunc = () =>
        {
            if (index >= measurements.Length)
            {
                index = 0;
            }

            return measurements[index++];
        };

        var observableUpDownCounter = meter.CreateObservableUpDownCounter<T>(Guid.NewGuid().ToString(), observableFunc);

        // One dimension
        metricCollector.CollectObservableInstruments();
        Assert.Equal(value, metricCollector.GetObservableUpDownCounterValue<T>(observableUpDownCounter.Name, new KeyValuePair<string, object?>(intDimension, intVal))!.Value);

        // Two dimensions
        metricCollector.CollectObservableInstruments();
        var actualValue = metricCollector.GetObservableUpDownCounterValue<T>(
            observableUpDownCounter.Name,
            new KeyValuePair<string, object?>(intDimension, intVal),
            new KeyValuePair<string, object?>(doubleDimension, doubleVal));
        Assert.Equal(value, actualValue!.Value);

        // Three dimensions
        metricCollector.CollectObservableInstruments();
        actualValue = metricCollector.GetObservableUpDownCounterValue<T>(observableUpDownCounter.Name,
                        new KeyValuePair<string, object?>(longDimension, longVal),
                        new KeyValuePair<string, object?>(intDimension, intVal),
                        new KeyValuePair<string, object?>(doubleDimension, doubleVal));
        Assert.Equal(value, actualValue!.Value);
    }

    private static void ObservableUpDownCounterWithArrayDimensionsTest<T>(Meter meter, MetricCollector metricCollector, T value)
        where T : struct
    {
        var intArrayDimension = Guid.NewGuid().ToString();
        int[] intArrVal = new[] { 12, 55, 2023 };
        var doubleArrayDimension = Guid.NewGuid().ToString();
        double[] doubleArrVal = new[] { 1111.9999d, 0, 3.1415 };
        var longArrayDimension = Guid.NewGuid().ToString();
        long[] longArrVal = new[] { 1_999_988_887_777_111L, 1_111_222_333_444_555L, 1_999_988_887_777_111L, 0 };

        var measurements = new[]
        {
            new Measurement<T>(value, new KeyValuePair<string, object?>(intArrayDimension, intArrVal)),
            new Measurement<T>(value, new KeyValuePair<string, object?>(intArrayDimension, intArrVal), new KeyValuePair<string, object?>(doubleArrayDimension, doubleArrVal)),
            new Measurement<T>(value, new KeyValuePair<string, object?>(intArrayDimension, intArrVal),
                new KeyValuePair<string, object?>(doubleArrayDimension, doubleArrVal),
                new KeyValuePair<string, object?>(longArrayDimension, longArrVal))
        };

        int index = 0;
        var observableFunc = () =>
        {
            if (index >= measurements.Length)
            {
                index = 0;
            }

            return measurements[index++];
        };

        var observableUpDownCounter = meter.CreateObservableUpDownCounter<T>(Guid.NewGuid().ToString(), observableFunc);

        // One dimension
        metricCollector.CollectObservableInstruments();
        Assert.Equal(value, metricCollector.GetObservableUpDownCounterValue<T>(observableUpDownCounter.Name, new KeyValuePair<string, object?>(intArrayDimension, intArrVal))!.Value);

        // Two dimensions
        metricCollector.CollectObservableInstruments();
        var actualValue = metricCollector.GetObservableUpDownCounterValue<T>(observableUpDownCounter.Name,
            new KeyValuePair<string, object?>(intArrayDimension, intArrVal),
            new KeyValuePair<string, object?>(doubleArrayDimension, doubleArrVal));

        Assert.Equal(value, actualValue!.Value);

        // Three dimensions
        metricCollector.CollectObservableInstruments();
        actualValue = metricCollector.GetObservableUpDownCounterValue<T>(observableUpDownCounter.Name,
            new KeyValuePair<string, object?>(longArrayDimension, longArrVal),
            new KeyValuePair<string, object?>(intArrayDimension, intArrVal),
            new KeyValuePair<string, object?>(doubleArrayDimension, doubleArrVal));

        Assert.Equal(value, actualValue!.Value);
    }
}
