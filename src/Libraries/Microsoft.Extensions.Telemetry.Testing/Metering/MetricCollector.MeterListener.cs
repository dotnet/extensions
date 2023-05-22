// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using Microsoft.Extensions.Telemetry.Testing.Metering;
using Microsoft.Extensions.Telemetry.Testing.Metering.Internal;

namespace Microsoft.Extensions.Telemetry.Testing.Metering;

public partial class MetricCollector
{
    private void OnInstrumentPublished(Instrument instrument, MeterListener listener)
    {
        var matchedByMeter = _meter is not null && ReferenceEquals(instrument.Meter, _meter);
        var matchedByMeterName = _meter is null && (_meterNames!.Length == 0 || _meterNames!.Any(x => x.StartsWith(instrument.Meter.Name, StringComparison.Ordinal)));

        if (matchedByMeter || matchedByMeterName)
        {
            RegisterInstrument(instrument);

            listener.EnableMeasurementEvents(instrument, this);
        }
    }

    private void CollectMeasurement<T>(Instrument instrument, T value, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        where T : struct
    {
        if (state == this)
        {
            var metricValuesHolder = GetMetricValuesHolder<T>(instrument);
            metricValuesHolder.ReceiveValue(value, tags);
        }
    }

    private void RegisterInstrument(Instrument instrument)
    {
        Type genericDefinedType = instrument.GetType().GetGenericTypeDefinition();

        AggregationType aggregationType = AggregationType.Save;
        Dictionary<Type, object> allValuesDictionary = null!;

        if (genericDefinedType == typeof(Counter<>))
        {
            aggregationType = AggregationType.Aggregate;
            allValuesDictionary = _allCounters;
        }
        else if (genericDefinedType == typeof(Histogram<>))
        {
            aggregationType = AggregationType.Save;
            allValuesDictionary = _allHistograms;
        }
        else if (genericDefinedType == typeof(UpDownCounter<>))
        {
            aggregationType = AggregationType.Aggregate;
            allValuesDictionary = _allUpDownCounters;
        }
        else if (genericDefinedType == typeof(ObservableCounter<>))
        {
            aggregationType = AggregationType.SaveOrUpdate;
            allValuesDictionary = _allObservableCounters;
        }
        else if (genericDefinedType == typeof(ObservableGauge<>))
        {
            aggregationType = AggregationType.Save;
            allValuesDictionary = _allObservableGauges;
        }
        else if (genericDefinedType == typeof(ObservableUpDownCounter<>))
        {
            aggregationType = AggregationType.SaveOrUpdate;
            allValuesDictionary = _allObservableUpDownCounters;
        }

        Type measurementValueType = instrument.GetType().GetGenericArguments()[0];

        if (measurementValueType == typeof(int))
        {
            var metricsValuesDictionary = (ConcurrentDictionary<string, MetricValuesHolder<int>>)allValuesDictionary[typeof(int)];
            _ = metricsValuesDictionary.GetOrAdd(instrument.Name, new MetricValuesHolder<int>(_timeProvider, aggregationType, instrument.Name));
        }
        else if (measurementValueType == typeof(byte))
        {
            var metricsValuesDictionary = (ConcurrentDictionary<string, MetricValuesHolder<byte>>)allValuesDictionary[typeof(byte)];
            _ = metricsValuesDictionary.GetOrAdd(instrument.Name, new MetricValuesHolder<byte>(_timeProvider, aggregationType, instrument.Name));
        }
        else if (measurementValueType == typeof(short))
        {
            var metricsValuesDictionary = (ConcurrentDictionary<string, MetricValuesHolder<short>>)allValuesDictionary[typeof(short)];
            _ = metricsValuesDictionary.GetOrAdd(instrument.Name, new MetricValuesHolder<short>(_timeProvider, aggregationType, instrument.Name));
        }
        else if (measurementValueType == typeof(long))
        {
            var metricsValuesDictionary = (ConcurrentDictionary<string, MetricValuesHolder<long>>)allValuesDictionary[typeof(long)];
            _ = metricsValuesDictionary.GetOrAdd(instrument.Name, new MetricValuesHolder<long>(_timeProvider, aggregationType, instrument.Name));
        }
        else if (measurementValueType == typeof(double))
        {
            var metricsValuesDictionary = (ConcurrentDictionary<string, MetricValuesHolder<double>>)allValuesDictionary[typeof(double)];
            _ = metricsValuesDictionary.GetOrAdd(instrument.Name, new MetricValuesHolder<double>(_timeProvider, aggregationType, instrument.Name));
        }
        else if (measurementValueType == typeof(float))
        {
            var metricsValuesDictionary = (ConcurrentDictionary<string, MetricValuesHolder<float>>)allValuesDictionary[typeof(float)];
            _ = metricsValuesDictionary.GetOrAdd(instrument.Name, new MetricValuesHolder<float>(_timeProvider, aggregationType, instrument.Name));
        }
        else if (measurementValueType == typeof(decimal))
        {
            var metricsValuesDictionary = (ConcurrentDictionary<string, MetricValuesHolder<decimal>>)allValuesDictionary[typeof(decimal)];
            _ = metricsValuesDictionary.GetOrAdd(instrument.Name, new MetricValuesHolder<decimal>(_timeProvider, aggregationType, instrument.Name));
        }
    }

    private MetricValuesHolder<T> GetMetricValuesHolder<T>(Instrument instrument)
        where T : struct
    {
        var instrumentType = instrument.GetType();

        Dictionary<Type, object> allValuesDictionary = null!;

        if (instrumentType == typeof(Counter<T>))
        {
            allValuesDictionary = _allCounters;
        }
        else if (instrumentType == typeof(Histogram<T>))
        {
            allValuesDictionary = _allHistograms;
        }
        else if (instrumentType == typeof(UpDownCounter<T>))
        {
            allValuesDictionary = _allUpDownCounters;
        }
        else if (instrumentType == typeof(ObservableCounter<T>))
        {
            allValuesDictionary = _allObservableCounters;
        }
        else if (instrumentType == typeof(ObservableGauge<T>))
        {
            allValuesDictionary = _allObservableGauges;
        }
        else if (instrumentType == typeof(ObservableUpDownCounter<T>))
        {
            allValuesDictionary = _allObservableUpDownCounters;
        }

        var metricsValuesDictionary = (ConcurrentDictionary<string, MetricValuesHolder<T>>)allValuesDictionary[typeof(T)];

        return metricsValuesDictionary[instrument.Name];
    }
}
