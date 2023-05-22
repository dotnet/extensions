// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Telemetry.Testing.Metering.Internal;

namespace Microsoft.Extensions.Telemetry.Testing.Metering;

/// <summary>
/// The metric measurements holder that contains information belonging to one named metric.
/// </summary>
/// <typeparam name="T">The type of metric measurement value.</typeparam>
[Experimental]
public sealed class MetricValuesHolder<T>
    where T : struct
{
    private static readonly HashSet<Type> _supportedValueTypesAsDimensionValue = new()
    {
        typeof(int),
        typeof(byte),
        typeof(short),
        typeof(long),
        typeof(float),
        typeof(double),
        typeof(char),
    };

    private readonly TimeProvider _timeProvider;
    private readonly AggregationType _aggregationType;
    private readonly ConcurrentDictionary<string, MetricValue<T>> _valuesTable;
#if NETCOREAPP3_1_OR_GREATER
    private readonly ConcurrentBag<MetricValue<T>> _values;
#else
    private ConcurrentBag<MetricValue<T>> _values;
#endif
    private string? _latestWrittenKey;

    internal MetricValuesHolder(TimeProvider timeProvider, AggregationType aggregationType, string metricName)
    {
        _timeProvider = timeProvider;
        _aggregationType = aggregationType;
        MetricName = metricName;
        _values = new();
        _valuesTable = new();
    }

    /// <summary>
    /// Gets the metric name.
    /// </summary>
    public string MetricName { get; }

    /// <summary>
    /// Gets all metric values recorded by the instrument.
    /// </summary>
    public IReadOnlyCollection<MetricValue<T>> AllValues => _values;

    /// <summary>
    /// Gets the latest recorded metric measurement value.
    /// </summary>
    public T? LatestWrittenValue => LatestWritten?.Value;

    /// <summary>
    /// Gets the <see cref="MetricValue{T}"/> object containing whole information about the latest recorded metric measurement.
    /// </summary>
    public MetricValue<T>? LatestWritten => _latestWrittenKey == null ? null : _valuesTable[_latestWrittenKey];

    /// <summary>
    /// Gets a recorded metric measurement value by given dimensions.
    /// </summary>
    /// <param name="tags">The dimensions of a metric measurement.</param>
    /// <returns>The metric measurement value or <see langword="null"/> if it does not exist.</returns>
    public T? GetValue(params KeyValuePair<string, object?>[] tags)
    {
        var tagsCopy = tags.ToArray();
        Array.Sort(tagsCopy, (x, y) => StringComparer.Ordinal.Compare(x.Key, y.Key));

        var key = CreateKey(tagsCopy);

        _ = _valuesTable.TryGetValue(key, out var value);

        return value?.Value;
    }

    /// <summary>
    /// Clears all metric measurements information.
    /// </summary>
    public void Clear()
    {
#if NETCOREAPP3_1_OR_GREATER
        _values.Clear();
#else
        _values = new();
#endif
        _valuesTable.Clear();
        _latestWrittenKey = null;
    }

    internal void ReceiveValue(T value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var tagsArray = tags.ToArray();
        Array.Sort(tagsArray, (x, y) => StringComparer.Ordinal.Compare(x.Key, y.Key));
        var key = CreateKey(tagsArray);

        switch (_aggregationType)
        {
            case AggregationType.Save:
                Save(value, tagsArray, key);
                break;

            case AggregationType.Aggregate:
                SaveAndAggregate(value, tagsArray, key);
                break;

            case AggregationType.SaveOrUpdate:
                SaveOrUpdate(value, tagsArray, key);
                break;

            default:
                throw new InvalidOperationException($"Aggregation type {_aggregationType} is not supported.");
        }
    }

    private static string CreateKey(params KeyValuePair<string, object?>[] tags)
    {
        if (tags.Length == 0)
        {
            return string.Empty;
        }

        const char TagSeparator = ';';
        const char KeyValueSeparator = ':';
        const char ArrayMemberSeparator = ',';

        var keyBuilder = new StringBuilder();

        foreach (var kvp in tags)
        {
            _ = keyBuilder
                .Append(kvp.Key)
                .Append(KeyValueSeparator);

            if (kvp.Value is null)
            {
                _ = keyBuilder.Append(string.Empty);
            }
            else
            {
                var valueType = kvp.Value.GetType();

                if (valueType == typeof(string) || (!valueType.IsArray && _supportedValueTypesAsDimensionValue.Contains(valueType)))
                {
                    _ = keyBuilder.Append(kvp.Value);
                }
                else if (valueType.IsArray && _supportedValueTypesAsDimensionValue.Contains(valueType.GetElementType()!))
                {
                    var array = (Array)kvp.Value;

                    _ = keyBuilder.Append('[');

                    foreach (var item in array)
                    {
                        _ = keyBuilder
                            .Append(item)
                            .Append(ArrayMemberSeparator);
                    }

                    _ = keyBuilder.Append(']');
                }
                else
                {
                    throw new InvalidOperationException($"The type {valueType.FullName} is not supported as a dimension value type.");
                }
            }

            _ = keyBuilder.Append(TagSeparator);
        }

        return keyBuilder.ToString();
    }

    private void Save(T value, KeyValuePair<string, object?>[] tagsArray, string key)
    {
        var metricValue = new MetricValue<T>(value, tagsArray, _timeProvider.GetUtcNow());

        _latestWrittenKey = key;

        SaveMetricValue(key, metricValue);
    }

    private void SaveAndAggregate(T value, KeyValuePair<string, object?>[] tagsArray, string key)
    {
        _latestWrittenKey = key;

        GetOrAdd(key, tagsArray).Add(value);
    }

    private void SaveOrUpdate(T value, KeyValuePair<string, object?>[] tagsArray, string key)
    {
        _latestWrittenKey = key;

        GetOrAdd(key, tagsArray).Update(value);
    }

    private MetricValue<T> GetOrAdd(string key, KeyValuePair<string, object?>[] tagsArray)
    {
        return _valuesTable.GetOrAdd(key,
            (_) =>
            {
                var metricValue = new MetricValue<T>(default, tagsArray, _timeProvider.GetUtcNow());
                _values.Add(metricValue);

                return metricValue;
            });
    }

    private void SaveMetricValue(string key, MetricValue<T> metricValue)
    {
        _values.Add(metricValue);
        _ = _valuesTable.AddOrUpdate(key, metricValue, (_, _) => metricValue);
    }
}
