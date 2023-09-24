// Assembly 'Microsoft.Extensions.Diagnostics.Testing'

using System.Collections.Generic;

namespace Microsoft.Extensions.Diagnostics.Metrics.Testing;

public static class MeasurementExtensions
{
    public static IEnumerable<CollectedMeasurement<T>> ContainsTags<T>(this IEnumerable<CollectedMeasurement<T>> measurements, params KeyValuePair<string, object?>[] tags) where T : struct;
    public static IEnumerable<CollectedMeasurement<T>> ContainsTags<T>(this IEnumerable<CollectedMeasurement<T>> measurements, params string[] tags) where T : struct;
    public static IEnumerable<CollectedMeasurement<T>> MatchesTags<T>(this IEnumerable<CollectedMeasurement<T>> measurements, params KeyValuePair<string, object?>[] tags) where T : struct;
    public static IEnumerable<CollectedMeasurement<T>> MatchesTags<T>(this IEnumerable<CollectedMeasurement<T>> measurements, params string[] tags) where T : struct;
    public static T EvaluateAsCounter<T>(this IEnumerable<CollectedMeasurement<T>> measurements) where T : struct;
}
