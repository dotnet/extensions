// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Microsoft.Extensions.Telemetry.Testing.Metering;

/// <summary>
/// Represents the whole information about a single metric measurement.
/// </summary>
/// <typeparam name="T">The type of metric measurement value.</typeparam>
[Experimental(diagnosticId: "NETEXT0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public sealed class MetricValue<T>
    where T : struct
{
    private int _isLockTaken;

    internal MetricValue(T measurement, KeyValuePair<string, object?>[] tags, DateTimeOffset timestamp)
    {
        Tags = tags;
        Timestamp = timestamp;
        Value = measurement;
    }

    /// <summary>
    /// Gets a measurement's value.
    /// </summary>
    public T Value { get; internal set; }

    /// <summary>
    /// Gets a timestamp indicating when a measurement was recorded.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets a collection of measurement's dimensions.
    /// </summary>
    public IReadOnlyCollection<KeyValuePair<string, object?>> Tags { get; }

    /// <summary>
    /// Gets a dimension value by a dimension name.
    /// </summary>
    /// <param name="dimensionName">The dimension name.</param>
    /// <returns>The dimension value or <see langword="null"/> if the dimension value was not recorded.</returns>
    public object? GetDimension(string dimensionName)
    {
        foreach (var kvp in Tags)
        {
            if (kvp.Key == dimensionName)
            {
                return kvp.Value;
            }
        }

        return null;
    }

    internal void Add(T value)
    {
        SafeUpdate(
            () =>
            {
                var valueObj = (object)Value;
                var valueToAddObj = (object)value;

#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
                object result = value switch
                {
                    byte => (byte)((byte)valueObj + (byte)valueToAddObj),
                    short => (short)((short)valueObj + (short)valueToAddObj),
                    int => (int)valueObj + (int)valueToAddObj,
                    long => (long)valueObj + (long)valueToAddObj,
                    float => (float)valueObj + (float)valueToAddObj,
                    double => (double)valueObj + (double)valueToAddObj,
                    decimal => (decimal)valueObj + (decimal)valueToAddObj,
                    _ => throw new InvalidOperationException($"The type {typeof(T).FullName} is not supported as a metering measurement value type."),
                };
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).

                Value = (T)result;
            });
    }

    internal void Update(T value)
    {
        SafeUpdate(() => Value = value);
    }

    [ExcludeFromCodeCoverage]
    private void SafeUpdate(Action action)
    {
        var sw = default(SpinWait);

        while (true)
        {
            if (Interlocked.Exchange(ref _isLockTaken, 1) == 0)
            {
                // Lock acquired
                action();

                // Release lock
                _ = Interlocked.Exchange(ref _isLockTaken, 0);
                break;
            }

            sw.SpinOnce();
        }
    }
}
