// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;

namespace Microsoft.Extensions.Telemetry.Testing.Metrics;

/// <summary>
/// Extensions to simplify working with lists of measurements.
/// </summary>
public static class MeasurementExtensions
{
    /// <summary>
    /// Filters a list of measurements based on subset tags matching.
    /// </summary>
    /// <typeparam name="T">The type of measurement value.</typeparam>
    /// <param name="measurements">The original full list of measurements.</param>
    /// <param name="tags">The set of tags to match against. Only measurements that have at least these matching tags are returned.</param>
    /// <returns>A list of matching measurements.</returns>
    public static IEnumerable<CollectedMeasurement<T>> ContainsTags<T>(this IEnumerable<CollectedMeasurement<T>> measurements, params KeyValuePair<string, object?>[] tags)
        where T : struct
        => measurements.Where(m => m.ContainsTags(tags));

    /// <summary>
    /// Filters a list of measurements based on subset tag matching.
    /// </summary>
    /// <typeparam name="T">The type of measurement value.</typeparam>
    /// <param name="measurements">The original full list of measurements.</param>
    /// <param name="tags">The set of tags to match against. Only measurements that have at least these matching tag names are returned.</param>
    /// <returns>A list of matching measurements.</returns>
    public static IEnumerable<CollectedMeasurement<T>> ContainsTags<T>(this IEnumerable<CollectedMeasurement<T>> measurements, params string[] tags)
        where T : struct
        => measurements.Where(m => m.ContainsTags(tags));

    /// <summary>
    /// Filters a list of measurements based on exact tag matching.
    /// </summary>
    /// <typeparam name="T">The type of measurement value.</typeparam>
    /// <param name="measurements">The original full list of measurements.</param>
    /// <param name="tags">The set of tags to match against. Only measurements that have exactly those matching tags are returned.</param>
    /// <returns>A list of matching measurements.</returns>
    public static IEnumerable<CollectedMeasurement<T>> MatchesTags<T>(this IEnumerable<CollectedMeasurement<T>> measurements, params KeyValuePair<string, object?>[] tags)
        where T : struct
        => measurements.Where(m => m.MatchesTags(tags));

    /// <summary>
    /// Filters a list of measurements based on exact tag name matching.
    /// </summary>
    /// <typeparam name="T">The type of measurement value.</typeparam>
    /// <param name="measurements">The original full list of measurements.</param>
    /// <param name="tags">The set of tags to match against. Only measurements that have exactly those matching tag names are returned.</param>
    /// <returns>A list of matching measurements.</returns>
    public static IEnumerable<CollectedMeasurement<T>> MatchesTags<T>(this IEnumerable<CollectedMeasurement<T>> measurements, params string[] tags)
        where T : struct
        => measurements.Where(m => m.MatchesTags(tags));

    /// <summary>
    /// Process the series of measurements adding all values together to produce a final count, identical to what a <see cref="Counter{T}" /> instrument would produce.
    /// </summary>
    /// <typeparam name="T">The type of measurement value.</typeparam>
    /// <param name="measurements">The list of measurements to process.</param>
    /// <returns>The resulting count.</returns>
    public static T EvaluateAsCounter<T>(this IEnumerable<CollectedMeasurement<T>> measurements)
        where T : struct
    {
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
        return measurements switch
        {
            IEnumerable<CollectedMeasurement<byte>> l => (T)(object)ByteSum(l),
            IEnumerable<CollectedMeasurement<short>> l => (T)(object)ShortSum(l),
            IEnumerable<CollectedMeasurement<int>> l => (T)(object)l.Sum(m => m.Value),
            IEnumerable<CollectedMeasurement<long>> l => (T)(object)l.Sum(m => m.Value),
            IEnumerable<CollectedMeasurement<float>> l => (T)(object)l.Sum(m => m.Value),
            IEnumerable<CollectedMeasurement<double>> l => (T)(object)l.Sum(m => m.Value),
            IEnumerable<CollectedMeasurement<decimal>> l => (T)(object)l.Sum(m => m.Value),
        };
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).

        static byte ByteSum(IEnumerable<CollectedMeasurement<byte>> measurements)
        {
            byte sum = 0;
            foreach (var measurement in measurements)
            {
                sum += measurement.Value;
            }

            return sum;
        }

        static short ShortSum(IEnumerable<CollectedMeasurement<short>> measurements)
        {
            short sum = 0;
            foreach (var measurement in measurements)
            {
                sum += measurement.Value;
            }

            return sum;
        }
    }
}
