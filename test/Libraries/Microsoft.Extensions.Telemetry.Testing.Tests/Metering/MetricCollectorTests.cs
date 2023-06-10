// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Testing.Metering.Test;

public partial class MetricCollectorTests
{
    [Fact]
    public void Ctor_Throws_WhenInvalidArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new MetricCollector((Meter)null!));
        Assert.Throws<ArgumentNullException>(() => new MetricCollector((IEnumerable<string>)null!));
        Assert.Throws<ArgumentNullException>(() => new MetricCollector((Instrument)null!));
    }

    [Fact]
    public void Measurements_AreFilteredOut_WithMeterNameFilter()
    {
        using var meter1 = new Meter(Guid.NewGuid().ToString());
        using var meter2 = new Meter(Guid.NewGuid().ToString());
        using var meterToIgnore = new Meter(Guid.NewGuid().ToString());

        using var metricCollector = new MetricCollector(new[] { meter1.Name, meter2.Name });

        const int IntValue1 = 999;
        meter1.CreateCounter<int>("int_counter1").Add(IntValue1);

        // Measurement is captured
        Assert.Equal(IntValue1, metricCollector.GetCounterValue<int>("int_counter1"));

        const double DoubleValue1 = 999;
        meter2.CreateHistogram<double>("double_histogram1").Record(DoubleValue1);

        // Measurement is captured
        Assert.Equal(DoubleValue1, metricCollector.GetHistogramValue<double>("double_histogram1"));

        const int IntValue2 = 999999;
        meterToIgnore.CreateCounter<int>("int_counter2").Add(IntValue2);

        // Measurement is filtered out
        Assert.Null(metricCollector.GetCounterValue<int>("int_counter2"));

        const double DoubleValue2 = 111.2222;
        meter2.CreateHistogram<double>("double_histogram2").Record(DoubleValue2);

        // Measurement is filtered out
        Assert.Null(metricCollector.GetCounterValue<int>("double_histogram2"));
    }

    [Fact]
    public void Measurements_SingleInstrument()
    {
        var meterName = Guid.NewGuid().ToString();
        using var meter1 = new Meter(meterName);
        using var meter2 = new Meter(meterName);

        const int IntValue1 = 123459;
        const int IntValue2 = 987654;

        var ctr1 = meter1.CreateCounter<int>("int_counter1");
        var ctr2 = meter2.CreateCounter<int>("int_counter2");

        using var metricCollector = new MetricCollector(ctr2);

        ctr1.Add(IntValue1);
        ctr2.Add(IntValue2);

        var x = metricCollector.GetAllCounters<int>();

        // Single measurement is captured
        Assert.Equal(1, metricCollector.Count);
        Assert.Equal(IntValue2, metricCollector.GetCounterValue<int>("int_counter2"));
    }

    [Fact]
    public void Measurements_AreFilteredOut_WithMeterFilter()
    {
        var meterName = Guid.NewGuid().ToString();
        using var meter1 = new Meter(meterName);
        using var meter2 = new Meter(meterName);

        using var metricCollector = new MetricCollector(meter1);

        const int IntValue1 = 123459;
        meter1.CreateCounter<int>("int_counter1").Add(IntValue1);

        // Measurement is captured
        Assert.Equal(IntValue1, metricCollector.GetCounterValue<int>("int_counter1"));

        const double DoubleValue1 = 987;
        meter1.CreateHistogram<double>("double_histogram1").Record(DoubleValue1);

        // Measurement is captured
        Assert.Equal(DoubleValue1, metricCollector.GetHistogramValue<double>("double_histogram1"));

        const int IntValue2 = 91111;
        meter2.CreateCounter<int>("int_counter2").Add(IntValue2);

        // Measurement is filtered out
        Assert.Null(metricCollector.GetCounterValue<int>("int_counter2"));

        const double DoubleValue2 = 333.7777;
        meter2.CreateHistogram<double>("double_histogram2").Record(DoubleValue2);

        // Measurement is filtered out
        Assert.Null(metricCollector.GetCounterValue<int>("double_histogram2"));
    }

    [Fact]
    public void GetCounterValues_ReturnsMeteringValuesHolder()
    {
        const long TestValue = 111;
        using var meter = new Meter(Guid.NewGuid().ToString());

        var counter = meter.CreateCounter<long>(Guid.NewGuid().ToString());

        using var metricCollector = new MetricCollector(meter);

        counter.Add(TestValue);

        var meteringHolder = metricCollector.GetCounterValues<long>(counter.Name);

        Assert.NotNull(meteringHolder);
        Assert.Equal(TestValue, meteringHolder.GetValue());
        Assert.Null(metricCollector.GetCounterValues<byte>(counter.Name));
        Assert.Null(metricCollector.GetCounterValues<short>(counter.Name));
        Assert.Null(metricCollector.GetCounterValues<int>(counter.Name));
        Assert.Null(metricCollector.GetCounterValues<float>(counter.Name));
        Assert.Null(metricCollector.GetCounterValues<double>(counter.Name));
        Assert.Null(metricCollector.GetCounterValues<decimal>(counter.Name));
    }

    [Fact]
    public void GetHistogramValues_ReturnsMeteringValuesHolder()
    {
        const int TestValue = 271;
        using var meter = new Meter(Guid.NewGuid().ToString());
        var histogram = meter.CreateHistogram<int>(Guid.NewGuid().ToString());
        using var metricCollector = new MetricCollector(meter);

        histogram.Record(TestValue);

        var meteringHolder = metricCollector.GetHistogramValues<int>(histogram.Name);

        Assert.NotNull(meteringHolder);
        Assert.Equal(TestValue, meteringHolder.GetValue());
        Assert.Null(metricCollector.GetHistogramValues<byte>(histogram.Name));
        Assert.Null(metricCollector.GetHistogramValues<short>(histogram.Name));
        Assert.Null(metricCollector.GetHistogramValues<long>(histogram.Name));
        Assert.Null(metricCollector.GetHistogramValues<float>(histogram.Name));
        Assert.Null(metricCollector.GetHistogramValues<double>(histogram.Name));
        Assert.Null(metricCollector.GetHistogramValues<decimal>(histogram.Name));
    }

    [Fact]
    public void GetObservableCounterValues_ReturnsMeteringValuesHolder()
    {
        const byte TestValue = 255;
        using var meter = new Meter(Guid.NewGuid().ToString());
        var observableCounter = meter.CreateObservableCounter<byte>(Guid.NewGuid().ToString(), () => TestValue);
        using var metricCollector = new MetricCollector(meter);

        metricCollector.CollectObservableInstruments();
        var meteringHolder = metricCollector.GetObservableCounterValues<byte>(observableCounter.Name);

        Assert.NotNull(meteringHolder);
        Assert.Equal(TestValue, meteringHolder.GetValue());
        Assert.Null(metricCollector.GetObservableCounterValues<short>(observableCounter.Name));
        Assert.Null(metricCollector.GetObservableCounterValues<int>(observableCounter.Name));
        Assert.Null(metricCollector.GetObservableCounterValues<long>(observableCounter.Name));
        Assert.Null(metricCollector.GetObservableCounterValues<float>(observableCounter.Name));
        Assert.Null(metricCollector.GetObservableCounterValues<double>(observableCounter.Name));
        Assert.Null(metricCollector.GetObservableCounterValues<decimal>(observableCounter.Name));
    }

    [Fact]
    public void GetUpDownCounterValues_ReturnsMeteringValuesHolder()
    {
        const short TestValue = 19999;
        using var meter = new Meter(Guid.NewGuid().ToString());
        var upDownCounter = meter.CreateUpDownCounter<short>(Guid.NewGuid().ToString());
        using var metricCollector = new MetricCollector(meter);

        upDownCounter.Add(TestValue);
        var meteringHolder = metricCollector.GetUpDownCounterValues<short>(upDownCounter.Name);

        Assert.NotNull(meteringHolder);
        Assert.Equal(TestValue, meteringHolder.GetValue());
        Assert.Null(metricCollector.GetUpDownCounterValues<byte>(upDownCounter.Name));
        Assert.Null(metricCollector.GetUpDownCounterValues<int>(upDownCounter.Name));
        Assert.Null(metricCollector.GetUpDownCounterValues<long>(upDownCounter.Name));
        Assert.Null(metricCollector.GetUpDownCounterValues<float>(upDownCounter.Name));
        Assert.Null(metricCollector.GetUpDownCounterValues<double>(upDownCounter.Name));
        Assert.Null(metricCollector.GetUpDownCounterValues<decimal>(upDownCounter.Name));
    }

    [Fact]
    public void GetObservableGaugeValues_ReturnsMeteringValuesHolder()
    {
        const long TestValue = 11_225_599L;
        using var meter = new Meter(Guid.NewGuid().ToString());
        var observableGauge = meter.CreateObservableGauge(Guid.NewGuid().ToString(), () => TestValue);
        using var metricCollector = new MetricCollector(meter);

        metricCollector.CollectObservableInstruments();
        var meteringHolder = metricCollector.GetObservableGaugeValues<long>(observableGauge.Name);

        Assert.NotNull(meteringHolder);
        Assert.Equal(TestValue, meteringHolder.GetValue());
        Assert.Null(metricCollector.GetObservableGaugeValues<short>(observableGauge.Name));
        Assert.Null(metricCollector.GetObservableGaugeValues<int>(observableGauge.Name));
        Assert.Null(metricCollector.GetObservableGaugeValues<byte>(observableGauge.Name));
        Assert.Null(metricCollector.GetObservableGaugeValues<float>(observableGauge.Name));
        Assert.Null(metricCollector.GetObservableGaugeValues<double>(observableGauge.Name));
        Assert.Null(metricCollector.GetObservableGaugeValues<decimal>(observableGauge.Name));
    }

    [Fact]
    public void GetObservableUpDownCounterValues_ReturnsMeteringValuesHolder()
    {
        const int TestValue = int.MaxValue;
        using var meter = new Meter(Guid.NewGuid().ToString());
        var observableUpDownCounter = meter.CreateObservableUpDownCounter(Guid.NewGuid().ToString(), () => TestValue);
        using var metricCollector = new MetricCollector(meter);

        metricCollector.CollectObservableInstruments();
        var meteringHolder = metricCollector.GetObservableUpDownCounterValues<int>(observableUpDownCounter.Name);

        Assert.NotNull(meteringHolder);
        Assert.Equal(TestValue, meteringHolder.GetValue());
        Assert.Null(metricCollector.GetObservableUpDownCounterValues<short>(observableUpDownCounter.Name));
        Assert.Null(metricCollector.GetObservableUpDownCounterValues<long>(observableUpDownCounter.Name));
        Assert.Null(metricCollector.GetObservableUpDownCounterValues<byte>(observableUpDownCounter.Name));
        Assert.Null(metricCollector.GetObservableUpDownCounterValues<float>(observableUpDownCounter.Name));
        Assert.Null(metricCollector.GetObservableUpDownCounterValues<double>(observableUpDownCounter.Name));
        Assert.Null(metricCollector.GetObservableUpDownCounterValues<decimal>(observableUpDownCounter.Name));
    }

    [Fact]
    public void GetCounterValue_ReturnsValue()
    {
        const long TestValue = 111;
        using var meter = new Meter(Guid.NewGuid().ToString());

        var counter = meter.CreateCounter<long>(Guid.NewGuid().ToString());

        using var metricCollector = new MetricCollector(meter);

        counter.Add(TestValue);

        Assert.Equal(TestValue, metricCollector.GetCounterValue<long>(counter.Name));
        Assert.Null(metricCollector.GetCounterValue<byte>(counter.Name));
        Assert.Null(metricCollector.GetCounterValue<short>(counter.Name));
        Assert.Null(metricCollector.GetCounterValue<int>(counter.Name));
        Assert.Null(metricCollector.GetCounterValue<float>(counter.Name));
        Assert.Null(metricCollector.GetCounterValue<double>(counter.Name));
        Assert.Null(metricCollector.GetCounterValue<decimal>(counter.Name));
    }

    [Fact]
    public void GetHistogramValue_ReturnsValue()
    {
        const int TestValue = 271;
        using var meter = new Meter(Guid.NewGuid().ToString());
        var histogram = meter.CreateHistogram<int>(Guid.NewGuid().ToString());
        using var metricCollector = new MetricCollector(meter);

        histogram.Record(TestValue);

        Assert.Equal(TestValue, metricCollector.GetHistogramValue<int>(histogram.Name));
        Assert.Null(metricCollector.GetHistogramValue<byte>(histogram.Name));
        Assert.Null(metricCollector.GetHistogramValue<short>(histogram.Name));
        Assert.Null(metricCollector.GetHistogramValue<long>(histogram.Name));
        Assert.Null(metricCollector.GetHistogramValue<float>(histogram.Name));
        Assert.Null(metricCollector.GetHistogramValue<double>(histogram.Name));
        Assert.Null(metricCollector.GetHistogramValue<decimal>(histogram.Name));
    }

    [Fact]
    public void GetObservableCounterValue_ReturnsValue()
    {
        const byte TestValue = 255;
        using var meter = new Meter(Guid.NewGuid().ToString());
        var observableCounter = meter.CreateObservableCounter(Guid.NewGuid().ToString(), () => TestValue);
        using var metricCollector = new MetricCollector(meter);

        metricCollector.CollectObservableInstruments();

        Assert.Equal(TestValue, metricCollector.GetObservableCounterValue<byte>(observableCounter.Name));
        Assert.Null(metricCollector.GetObservableCounterValue<short>(observableCounter.Name));
        Assert.Null(metricCollector.GetObservableCounterValue<int>(observableCounter.Name));
        Assert.Null(metricCollector.GetObservableCounterValue<long>(observableCounter.Name));
        Assert.Null(metricCollector.GetObservableCounterValue<float>(observableCounter.Name));
        Assert.Null(metricCollector.GetObservableCounterValue<double>(observableCounter.Name));
        Assert.Null(metricCollector.GetObservableCounterValue<decimal>(observableCounter.Name));
    }

    [Fact]
    public void GetUpDownCounterValue_ReturnsValue()
    {
        const short TestValue = 19999;
        using var meter = new Meter(Guid.NewGuid().ToString());
        var upDownCounter = meter.CreateUpDownCounter<short>(Guid.NewGuid().ToString());
        using var metricCollector = new MetricCollector(meter);

        upDownCounter.Add(TestValue);

        Assert.Equal(TestValue, metricCollector.GetUpDownCounterValue<short>(upDownCounter.Name));
        Assert.Null(metricCollector.GetUpDownCounterValue<byte>(upDownCounter.Name));
        Assert.Null(metricCollector.GetUpDownCounterValue<int>(upDownCounter.Name));
        Assert.Null(metricCollector.GetUpDownCounterValue<long>(upDownCounter.Name));
        Assert.Null(metricCollector.GetUpDownCounterValue<float>(upDownCounter.Name));
        Assert.Null(metricCollector.GetUpDownCounterValue<double>(upDownCounter.Name));
        Assert.Null(metricCollector.GetUpDownCounterValue<decimal>(upDownCounter.Name));
    }

    [Fact]
    public void GetObservableGaugeValue_ReturnsValue()
    {
        const long TestValue = 11_225_599L;
        using var meter = new Meter(Guid.NewGuid().ToString());
        var observableGauge = meter.CreateObservableGauge(Guid.NewGuid().ToString(), () => TestValue);
        using var metricCollector = new MetricCollector(meter);

        metricCollector.CollectObservableInstruments();

        Assert.Equal(TestValue, metricCollector.GetObservableGaugeValue<long>(observableGauge.Name));
        Assert.Null(metricCollector.GetObservableGaugeValue<short>(observableGauge.Name));
        Assert.Null(metricCollector.GetObservableGaugeValue<int>(observableGauge.Name));
        Assert.Null(metricCollector.GetObservableGaugeValue<byte>(observableGauge.Name));
        Assert.Null(metricCollector.GetObservableGaugeValue<float>(observableGauge.Name));
        Assert.Null(metricCollector.GetObservableGaugeValue<double>(observableGauge.Name));
        Assert.Null(metricCollector.GetObservableGaugeValue<decimal>(observableGauge.Name));
    }

    [Fact]
    public void GetObservableUpDownCounterValue_ReturnsValue()
    {
        const int TestValue = int.MaxValue;
        using var meter = new Meter(Guid.NewGuid().ToString());
        var observableUpDownCounter = meter.CreateObservableUpDownCounter(Guid.NewGuid().ToString(), () => TestValue);
        using var metricCollector = new MetricCollector(meter);

        metricCollector.CollectObservableInstruments();

        Assert.Equal(TestValue, metricCollector.GetObservableUpDownCounterValue<int>(observableUpDownCounter.Name));
        Assert.Null(metricCollector.GetObservableUpDownCounterValue<short>(observableUpDownCounter.Name));
        Assert.Null(metricCollector.GetObservableUpDownCounterValue<long>(observableUpDownCounter.Name));
        Assert.Null(metricCollector.GetObservableUpDownCounterValue<byte>(observableUpDownCounter.Name));
        Assert.Null(metricCollector.GetObservableUpDownCounterValue<float>(observableUpDownCounter.Name));
        Assert.Null(metricCollector.GetObservableUpDownCounterValue<double>(observableUpDownCounter.Name));
        Assert.Null(metricCollector.GetObservableUpDownCounterValue<decimal>(observableUpDownCounter.Name));
    }

    [Fact]
    public void Clear_RemovesAllMeasurements()
    {
        var meterName = Guid.NewGuid().ToString();

        using var meter = new Meter(meterName);
        using var metricCollector = new MetricCollector(new[] { meterName });

        const int CounterValue = 1;
        meter.CreateCounter<int>("int_counter").Add(CounterValue);

        const int HistogramValue = 2;
        meter.CreateHistogram<int>("int_histogram").Record(HistogramValue);

        const long UpDownCounterValue = -999L;
        meter.CreateUpDownCounter<long>("long_updownCounter").Add(UpDownCounterValue);

        const short ObservableCounterValue = short.MaxValue;
        meter.CreateObservableCounter("short_observable_counter", () => ObservableCounterValue);

        const decimal ObservableGaugeValue = decimal.MinValue;
        meter.CreateObservableGauge("decimal_observable_gauge", () => ObservableGaugeValue);

        const double ObservableUpdownCouterValue = double.MaxValue;
        meter.CreateObservableUpDownCounter("double_observable_updownCounter", () => ObservableUpdownCouterValue);

        Assert.Equal(CounterValue, metricCollector.GetCounterValue<int>("int_counter")!.Value);
        Assert.Equal(HistogramValue, metricCollector.GetHistogramValue<int>("int_histogram")!.Value);
        Assert.Equal(UpDownCounterValue, metricCollector.GetUpDownCounterValue<long>("long_updownCounter")!.Value);

        metricCollector.CollectObservableInstruments();

        Assert.Equal(ObservableCounterValue, metricCollector.GetObservableCounterValue<short>("short_observable_counter")!.Value);
        Assert.Equal(ObservableGaugeValue, metricCollector.GetObservableGaugeValue<decimal>("decimal_observable_gauge")!.Value);
        Assert.Equal(ObservableUpdownCouterValue, metricCollector.GetObservableUpDownCounterValue<double>("double_observable_updownCounter")!.Value);

        metricCollector.Clear();

        Assert.Null(metricCollector.GetCounterValue<int>("int_counter"));
        Assert.Null(metricCollector.GetHistogramValue<int>("int_histogram"));
        Assert.Null(metricCollector.GetUpDownCounterValue<long>("long_updownCounter"));
        Assert.Null(metricCollector.GetObservableCounterValue<short>("short_observable_counter"));
        Assert.Null(metricCollector.GetObservableGaugeValue<decimal>("decimal_observable_gauge"));
        Assert.Null(metricCollector.GetObservableUpDownCounterValue<double>("double_observable_updownCounter"));
    }

    [Fact]
    public void CollectObservableInstruments_RecordsObservableMetrics()
    {
        using var meter = new Meter(Guid.NewGuid().ToString());
        using var metricCollector = new MetricCollector(meter);

        const int CounterValue = 47_382_492;
        const float UpDownCounterValue = 921.342f;
        const decimal GaugeValue = 12340m;

        meter.CreateObservableCounter("ObservableCounter", () => CounterValue);
        meter.CreateObservableGauge("ObservableGauge", () => GaugeValue);
        meter.CreateObservableUpDownCounter("ObservableUpDownCounter", () => UpDownCounterValue);

        // Observable instruments are not recorded
        Assert.Null(metricCollector.GetObservableCounterValue<int>("ObservableCounter"));
        Assert.Null(metricCollector.GetObservableGaugeValue<decimal>("ObservableGauge"));
        Assert.Null(metricCollector.GetObservableUpDownCounterValue<float>("ObservableUpDownCounter"));

        // Force recording of observable instruments
        metricCollector.CollectObservableInstruments();

        Assert.Equal(CounterValue, metricCollector.GetObservableCounterValue<int>("ObservableCounter"));
        Assert.Equal(GaugeValue, metricCollector.GetObservableGaugeValue<decimal>("ObservableGauge"));
        Assert.Equal(UpDownCounterValue, metricCollector.GetObservableUpDownCounterValue<float>("ObservableUpDownCounter"));
    }

    [Fact]
    public void GetXxxValue_ThrowsWhenInvalidValueTypeIsUsed()
    {
        using var metricCollector = new MetricCollector();

        var ex = Assert.Throws<InvalidOperationException>(() => metricCollector.GetCounterValue<ushort>(string.Empty));
        Assert.Equal($"The type {typeof(ushort).FullName} is not supported as a type for a metric measurement value", ex.Message);

        var ex1 = Assert.Throws<InvalidOperationException>(() => metricCollector.GetHistogramValue<ulong>(string.Empty));
        Assert.Equal($"The type {typeof(ulong).FullName} is not supported as a type for a metric measurement value", ex1.Message);

        var ex2 = Assert.Throws<InvalidOperationException>(() => metricCollector.GetUpDownCounterValue<sbyte>(string.Empty));
        Assert.Equal($"The type {typeof(sbyte).FullName} is not supported as a type for a metric measurement value", ex2.Message);

        var ex3 = Assert.Throws<InvalidOperationException>(() => metricCollector.GetObservableCounterValue<uint>(string.Empty));
        Assert.Equal($"The type {typeof(uint).FullName} is not supported as a type for a metric measurement value", ex3.Message);

        var ex4 = Assert.Throws<InvalidOperationException>(() => metricCollector.GetObservableGaugeValue<uint>(string.Empty));
        Assert.Equal($"The type {typeof(uint).FullName} is not supported as a type for a metric measurement value", ex4.Message);

        var ex5 = Assert.Throws<InvalidOperationException>(() => metricCollector.GetObservableUpDownCounterValue<uint>(string.Empty));
        Assert.Equal($"The type {typeof(uint).FullName} is not supported as a type for a metric measurement value", ex5.Message);
    }

    [Fact]
    public void GetXxxValues_ThrowsWhenInvalidValueTypeIsUsed()
    {
        using var metricCollector = new MetricCollector();

        var ex = Assert.Throws<InvalidOperationException>(() => metricCollector.GetCounterValues<ushort>(string.Empty));
        Assert.Equal($"The type {typeof(ushort).FullName} is not supported as a type for a metric measurement value", ex.Message);

        var ex1 = Assert.Throws<InvalidOperationException>(() => metricCollector.GetHistogramValues<ulong>(string.Empty));
        Assert.Equal($"The type {typeof(ulong).FullName} is not supported as a type for a metric measurement value", ex1.Message);

        var ex2 = Assert.Throws<InvalidOperationException>(() => metricCollector.GetUpDownCounterValues<sbyte>(string.Empty));
        Assert.Equal($"The type {typeof(sbyte).FullName} is not supported as a type for a metric measurement value", ex2.Message);

        var ex3 = Assert.Throws<InvalidOperationException>(() => metricCollector.GetObservableCounterValues<uint>(string.Empty));
        Assert.Equal($"The type {typeof(uint).FullName} is not supported as a type for a metric measurement value", ex3.Message);

        var ex4 = Assert.Throws<InvalidOperationException>(() => metricCollector.GetObservableGaugeValues<ulong>(string.Empty));
        Assert.Equal($"The type {typeof(ulong).FullName} is not supported as a type for a metric measurement value", ex4.Message);

        var ex5 = Assert.Throws<InvalidOperationException>(() => metricCollector.GetObservableUpDownCounterValues<ushort>(string.Empty));
        Assert.Equal($"The type {typeof(ushort).FullName} is not supported as a type for a metric measurement value", ex5.Message);
    }

    [Fact]
    public void GenericMetricCollector_CapturesFilteredMetering()
    {
        const int TestValue = 10;
        using var metricCollector = new MetricCollector<MetricCollectorTests>();
        using var meter = new Meter(typeof(MetricCollectorTests).FullName!);
        using var meterToIgnore = new Meter(Guid.NewGuid().ToString());

        var counter1 = meter.CreateCounter<int>(Guid.NewGuid().ToString());
        var counter2 = meterToIgnore.CreateCounter<int>(Guid.NewGuid().ToString());

        Assert.NotNull(metricCollector.GetCounterValues<int>(counter1.Name));
        Assert.Null(metricCollector.GetCounterValues<int>(counter2.Name));

        counter1.Add(TestValue);
        counter2.Add(TestValue);

        Assert.NotNull(metricCollector.GetCounterValue<int>(counter1.Name));
        Assert.Null(metricCollector.GetCounterValues<int>(counter2.Name));
    }

    [Fact]
    public void GetAllCounters_ReturnsAllCounters()
    {
        const long TestValue = 111;
        using var meter = new Meter(Guid.NewGuid().ToString());
        using var metricCollector = new MetricCollector(meter);

        var counter1 = meter.CreateCounter<long>(Guid.NewGuid().ToString());
        var counter2 = meter.CreateCounter<long>(Guid.NewGuid().ToString());
        var counter3 = meter.CreateCounter<long>(Guid.NewGuid().ToString());

        counter1.Add(TestValue);
        counter2.Add(TestValue);
        counter3.Add(TestValue);

        Assert.Equal(3, metricCollector.GetAllCounters<long>()!.Count);
    }

    [Fact]
    public void GetAllUpDownCounters_ReturnsAllCounters()
    {
        const long TestValue = 111;
        using var meter = new Meter(Guid.NewGuid().ToString());
        using var metricCollector = new MetricCollector(meter);

        var counter1 = meter.CreateUpDownCounter<long>(Guid.NewGuid().ToString());
        var counter2 = meter.CreateUpDownCounter<long>(Guid.NewGuid().ToString());
        var counter3 = meter.CreateUpDownCounter<long>(Guid.NewGuid().ToString());

        counter1.Add(TestValue);
        counter2.Add(TestValue);
        counter3.Add(TestValue);

        Assert.Equal(3, metricCollector.GetAllUpDownCounters<long>()!.Count);
    }

    [Fact]
    public void GetAllHistograms_ReturnsAllHistograms()
    {
        const long TestValue = 111;
        using var meter = new Meter(Guid.NewGuid().ToString());
        using var metricCollector = new MetricCollector(meter);

        var counter1 = meter.CreateHistogram<long>(Guid.NewGuid().ToString());
        var counter2 = meter.CreateHistogram<long>(Guid.NewGuid().ToString());
        var counter3 = meter.CreateHistogram<long>(Guid.NewGuid().ToString());

        counter1.Record(TestValue);
        counter2.Record(TestValue);
        counter3.Record(TestValue);

        Assert.Equal(3, metricCollector.GetAllHistograms<long>()!.Count);
    }

    [Fact]
    public void GetAllGauges_ReturnsAllGauges()
    {
        const long TestValue = 111;
        using var meter = new Meter(Guid.NewGuid().ToString());
        using var metricCollector = new MetricCollector(meter);
        _ = meter.CreateObservableGauge(Guid.NewGuid().ToString(), () => TestValue);
        _ = meter.CreateObservableGauge(Guid.NewGuid().ToString(), () => TestValue);
        _ = meter.CreateObservableGauge(Guid.NewGuid().ToString(), () => TestValue);

        // Force recording of observable instruments
        metricCollector.CollectObservableInstruments();

        Assert.Equal(3, metricCollector.GetAllObservableGauges<long>()!.Count);
    }

    [Fact]
    public void GetAllObservableCounters_ReturnsAllObservableCounters()
    {
        const long TestValue = 111;
        using var meter = new Meter(Guid.NewGuid().ToString());
        using var metricCollector = new MetricCollector(meter);

        _ = meter.CreateObservableCounter(Guid.NewGuid().ToString(), () => TestValue);
        _ = meter.CreateObservableCounter(Guid.NewGuid().ToString(), () => TestValue);
        _ = meter.CreateObservableCounter(Guid.NewGuid().ToString(), () => TestValue);

        // Force recording of observable instruments
        metricCollector.CollectObservableInstruments();

        Assert.Equal(3, metricCollector.GetAllObservableCounters<long>()!.Count);
    }

    [Fact]
    public void GetAllObservableUpDownCounters_ReturnsAllObservableUpDownCounters()
    {
        const long TestValue = 111;
        using var meter = new Meter(Guid.NewGuid().ToString());
        using var metricCollector = new MetricCollector(meter);

        _ = meter.CreateObservableUpDownCounter(Guid.NewGuid().ToString(), () => TestValue);
        _ = meter.CreateObservableUpDownCounter(Guid.NewGuid().ToString(), () => TestValue);
        _ = meter.CreateObservableUpDownCounter(Guid.NewGuid().ToString(), () => TestValue);

        // Force recording of observable instruments
        metricCollector.CollectObservableInstruments();

        Assert.Equal(3, metricCollector.GetAllObservableUpDownCounters<long>()!.Count);
    }

    [Fact]
    public void GetAllCounters_WithoutUnsupportedT_Throws()
    {
        using var meter = new Meter(Guid.NewGuid().ToString());
        using var metricCollector = new MetricCollector(meter);

        Assert.Throws<InvalidOperationException>(() => metricCollector.GetAllCounters<TagList>());
    }
}
