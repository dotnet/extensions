// Assembly 'Microsoft.Extensions.Telemetry.Testing'

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Telemetry.Testing.Metering;

[Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public static class MeasurementExtensions
{
    public static IEnumerable<CollectedMeasurement<T>> ContainsTags<T>(this IEnumerable<CollectedMeasurement<T>> measurements, params KeyValuePair<string, object?>[] tags) where T : struct;
    public static IEnumerable<CollectedMeasurement<T>> ContainsTags<T>(this IEnumerable<CollectedMeasurement<T>> measurements, params string[] tags) where T : struct;
    public static IEnumerable<CollectedMeasurement<T>> MatchesTags<T>(this IEnumerable<CollectedMeasurement<T>> measurements, params KeyValuePair<string, object?>[] tags) where T : struct;
    public static IEnumerable<CollectedMeasurement<T>> MatchesTags<T>(this IEnumerable<CollectedMeasurement<T>> measurements, params string[] tags) where T : struct;
    public static T EvaluateAsCounter<T>(this IEnumerable<CollectedMeasurement<T>> measurements) where T : struct;
}
