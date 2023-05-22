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
    public void ObservableGauge_BasicTest()
    {
        using var metricCollector = new MetricCollector();
        using var meter = new Meter(string.Empty);

        ObservableGaugeBasicTest<byte>(meter, metricCollector, 2, 5, 10, 17);
        ObservableGaugeBasicTest<short>(meter, metricCollector, 20, 50, 100, 170);
        ObservableGaugeBasicTest<int>(meter, metricCollector, 200, 500, 1000, 1700);
        ObservableGaugeBasicTest<long>(meter, metricCollector, 2000L, 5000L, 10000L, 17000L);
        ObservableGaugeBasicTest<float>(meter, metricCollector, 1.22f, 3.44f, 0, 1.22f + 3.44f);
        ObservableGaugeBasicTest<double>(meter, metricCollector, 5.22, 6.44, 10, 5.22 + 6.44 + 10);
        ObservableGaugeBasicTest<decimal>(meter, metricCollector, 0.99m, 15, 25.99m, 41.98m);
    }

    [Fact]
    public void ObservableGauge_StringDimensionsAreHandled()
    {
        using var metricCollector = new MetricCollector();
        using var meter = new Meter(string.Empty);

        ObservableGaugeWithStringDimensionsTest(meter, metricCollector, byte.MinValue);
        ObservableGaugeWithStringDimensionsTest(meter, metricCollector, short.MaxValue);
        ObservableGaugeWithStringDimensionsTest(meter, metricCollector, int.MaxValue);
        ObservableGaugeWithStringDimensionsTest(meter, metricCollector, long.MaxValue);
        ObservableGaugeWithStringDimensionsTest(meter, metricCollector, float.MaxValue);
        ObservableGaugeWithStringDimensionsTest(meter, metricCollector, double.MaxValue);
        ObservableGaugeWithStringDimensionsTest(meter, metricCollector, decimal.MaxValue);
    }

    [Fact]
    public void ObservableGauge_NumericDimensionsAreHandled()
    {
        using var metricCollector = new MetricCollector();
        using var meter = new Meter(string.Empty);

        ObservableGaugeWithNumericDimensionsTest(meter, metricCollector, byte.MinValue);
        ObservableGaugeWithNumericDimensionsTest(meter, metricCollector, short.MaxValue);
        ObservableGaugeWithNumericDimensionsTest(meter, metricCollector, int.MaxValue);
        ObservableGaugeWithNumericDimensionsTest(meter, metricCollector, long.MaxValue);
        ObservableGaugeWithNumericDimensionsTest(meter, metricCollector, float.MaxValue);
        ObservableGaugeWithNumericDimensionsTest(meter, metricCollector, double.MaxValue);
        ObservableGaugeWithNumericDimensionsTest(meter, metricCollector, decimal.MaxValue);
    }

    [Fact]
    public void ObservableGauge_ArrayDimensionsAreHandled()
    {
        using var metricCollector = new MetricCollector();
        using var meter = new Meter(string.Empty);

        ObservableGaugeWithArrayDimensionsTest(meter, metricCollector, byte.MinValue);
        ObservableGaugeWithArrayDimensionsTest(meter, metricCollector, short.MaxValue);
        ObservableGaugeWithArrayDimensionsTest(meter, metricCollector, int.MaxValue);
        ObservableGaugeWithArrayDimensionsTest(meter, metricCollector, long.MaxValue);
        ObservableGaugeWithArrayDimensionsTest(meter, metricCollector, float.MaxValue);
        ObservableGaugeWithArrayDimensionsTest(meter, metricCollector, double.MaxValue);
        ObservableGaugeWithArrayDimensionsTest(meter, metricCollector, decimal.MaxValue);
    }

    private static void ObservableGaugeBasicTest<T>(Meter meter, MetricCollector metricCollector, T value1, T value2, T value3, T value4)
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

        var observableGauge1 = meter.CreateObservableGauge<T>(Guid.NewGuid().ToString(), observableFunc);
        var holder1 = metricCollector.GetObservableGaugeValues<T>(observableGauge1.Name);

        Assert.NotNull(holder1);

        metricCollector.CollectObservableInstruments();
        var recordedValue1 = metricCollector.GetObservableGaugeValue<T>(observableGauge1.Name);

        Assert.NotNull(recordedValue1);
        Assert.Equal(value1, recordedValue1.Value);

        metricCollector.CollectObservableInstruments();

        Assert.Equal(value2, metricCollector.GetObservableGaugeValue<T>(observableGauge1.Name)!.Value);

        metricCollector.CollectObservableInstruments();

        Assert.Equal(value3, metricCollector.GetObservableGaugeValue<T>(observableGauge1.Name)!.Value);

        metricCollector.CollectObservableInstruments();

        Assert.Equal(value4, metricCollector.GetObservableGaugeValue<T>(observableGauge1.Name)!.Value);

        int index2 = 0;
        var observableFunc2 = () =>
        {
            if (index2 >= states.Length)
            {
                index2 = 0;
            }

            return states[index2++];
        };

        var observableGauge2 = meter.CreateObservableGauge<T>(Guid.NewGuid().ToString(), observableFunc2);
        var holder2 = metricCollector.GetObservableGaugeValues<T>(observableGauge2.Name);

        Assert.NotNull(holder2);

        metricCollector.CollectObservableInstruments();

        Assert.Equal(value1, metricCollector.GetObservableGaugeValue<T>(observableGauge2.Name)!.Value);

        metricCollector.CollectObservableInstruments();

        Assert.Equal(value2, metricCollector.GetObservableGaugeValue<T>(observableGauge2.Name)!.Value);

        metricCollector.CollectObservableInstruments();

        Assert.Equal(value3, metricCollector.GetObservableGaugeValue<T>(observableGauge2.Name)!.Value);

        metricCollector.CollectObservableInstruments();

        Assert.Equal(value4, metricCollector.GetObservableGaugeValue<T>(observableGauge2.Name)!.Value);
    }

    private static void ObservableGaugeWithStringDimensionsTest<T>(Meter meter, MetricCollector metricCollector, T value)
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

        var observableGauge = meter.CreateObservableGauge<T>(Guid.NewGuid().ToString(), observableFunc);

        // No dimensions
        metricCollector.CollectObservableInstruments();
        Assert.Equal(value, metricCollector.GetObservableGaugeValue<T>(observableGauge.Name)!.Value);

        // One dimension
        metricCollector.CollectObservableInstruments();
        Assert.Equal(value, metricCollector.GetObservableGaugeValue<T>(observableGauge.Name, new KeyValuePair<string, object?>(dimension1, dimension1Val))!.Value);

        // Two dimensions
        metricCollector.CollectObservableInstruments();
        var actualvalue = metricCollector.GetObservableGaugeValue<T>(
            observableGauge.Name,
            new KeyValuePair<string, object?>(dimension2, dimension2Val),
            new KeyValuePair<string, object?>(dimension1, dimension1Val));
        Assert.Equal(value, actualvalue!.Value);
    }

    private static void ObservableGaugeWithNumericDimensionsTest<T>(Meter meter, MetricCollector metricCollector, T value)
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

        var observableGauge = meter.CreateObservableGauge(Guid.NewGuid().ToString(), observableFunc);

        // One dimension
        metricCollector.CollectObservableInstruments();
        Assert.Equal(value, metricCollector.GetObservableGaugeValue<T>(observableGauge.Name, new KeyValuePair<string, object?>(intDimension, intVal))!.Value);

        // Two dimensions
        metricCollector.CollectObservableInstruments();
        var actualValue = metricCollector.GetObservableGaugeValue<T>(
            observableGauge.Name,
            new KeyValuePair<string, object?>(intDimension, intVal),
            new KeyValuePair<string, object?>(doubleDimension, doubleVal));
        Assert.Equal(value, actualValue!.Value);

        // Three dimensions
        metricCollector.CollectObservableInstruments();
        actualValue = metricCollector.GetObservableGaugeValue<T>(observableGauge.Name,
                        new KeyValuePair<string, object?>(longDimension, longVal),
                        new KeyValuePair<string, object?>(intDimension, intVal),
                        new KeyValuePair<string, object?>(doubleDimension, doubleVal));
        Assert.Equal(value, actualValue!.Value);
    }

    private static void ObservableGaugeWithArrayDimensionsTest<T>(Meter meter, MetricCollector metricCollector, T value)
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

        var observableGauge = meter.CreateObservableGauge<T>(Guid.NewGuid().ToString(), observableFunc);

        // One dimension
        metricCollector.CollectObservableInstruments();
        Assert.Equal(value, metricCollector.GetObservableGaugeValue<T>(observableGauge.Name, new KeyValuePair<string, object?>(intArrayDimension, intArrVal))!.Value);

        // Two dimensions
        metricCollector.CollectObservableInstruments();
        var actualValue = metricCollector.GetObservableGaugeValue<T>(observableGauge.Name,
            new KeyValuePair<string, object?>(intArrayDimension, intArrVal),
            new KeyValuePair<string, object?>(doubleArrayDimension, doubleArrVal));

        Assert.Equal(value, actualValue!.Value);

        // Three dimensions
        metricCollector.CollectObservableInstruments();
        actualValue = metricCollector.GetObservableGaugeValue<T>(observableGauge.Name,
            new KeyValuePair<string, object?>(longArrayDimension, longArrVal),
            new KeyValuePair<string, object?>(intArrayDimension, intArrVal),
            new KeyValuePair<string, object?>(doubleArrayDimension, doubleArrVal));

        Assert.Equal(value, actualValue!.Value);
    }
}
