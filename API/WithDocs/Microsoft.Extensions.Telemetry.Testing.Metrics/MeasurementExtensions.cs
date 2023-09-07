// Assembly 'Microsoft.Extensions.Telemetry.Testing'

using System.Collections.Generic;

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
    public static IEnumerable<CollectedMeasurement<T>> ContainsTags<T>(this IEnumerable<CollectedMeasurement<T>> measurements, params KeyValuePair<string, object?>[] tags) where T : struct;

    /// <summary>
    /// Filters a list of measurements based on subset tag matching.
    /// </summary>
    /// <typeparam name="T">The type of measurement value.</typeparam>
    /// <param name="measurements">The original full list of measurements.</param>
    /// <param name="tags">The set of tags to match against. Only measurements that have at least these matching tag names are returned.</param>
    /// <returns>A list of matching measurements.</returns>
    public static IEnumerable<CollectedMeasurement<T>> ContainsTags<T>(this IEnumerable<CollectedMeasurement<T>> measurements, params string[] tags) where T : struct;

    /// <summary>
    /// Filters a list of measurements based on exact tag matching.
    /// </summary>
    /// <typeparam name="T">The type of measurement value.</typeparam>
    /// <param name="measurements">The original full list of measurements.</param>
    /// <param name="tags">The set of tags to match against. Only measurements that have exactly those matching tags are returned.</param>
    /// <returns>A list of matching measurements.</returns>
    public static IEnumerable<CollectedMeasurement<T>> MatchesTags<T>(this IEnumerable<CollectedMeasurement<T>> measurements, params KeyValuePair<string, object?>[] tags) where T : struct;

    /// <summary>
    /// Filters a list of measurements based on exact tag name matching.
    /// </summary>
    /// <typeparam name="T">The type of measurement value.</typeparam>
    /// <param name="measurements">The original full list of measurements.</param>
    /// <param name="tags">The set of tags to match against. Only measurements that have exactly those matching tag names are returned.</param>
    /// <returns>A list of matching measurements.</returns>
    public static IEnumerable<CollectedMeasurement<T>> MatchesTags<T>(this IEnumerable<CollectedMeasurement<T>> measurements, params string[] tags) where T : struct;

    /// <summary>
    /// Process the series of measurements adding all values together to produce a final count, identical to what a <see cref="T:System.Diagnostics.Metrics.Counter`1" /> instrument would produce.
    /// </summary>
    /// <typeparam name="T">The type of measurement value.</typeparam>
    /// <param name="measurements">The list of measurements to process.</param>
    /// <returns>The resulting count.</returns>
    public static T EvaluateAsCounter<T>(this IEnumerable<CollectedMeasurement<T>> measurements) where T : struct;
}
