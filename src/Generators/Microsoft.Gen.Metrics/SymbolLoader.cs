// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Gen.Metrics;

internal static class SymbolLoader
{
    internal const string CounterTAttribute = "Microsoft.Extensions.Telemetry.Metrics.CounterAttribute`1";
    internal const string HistogramTAttribute = "Microsoft.Extensions.Telemetry.Metrics.HistogramAttribute`1";
    internal const string GaugeAttribute = "Microsoft.Extensions.Telemetry.Metrics.GaugeAttribute";
    internal const string CounterAttribute = "Microsoft.Extensions.Telemetry.Metrics.CounterAttribute";
    internal const string HistogramAttribute = "Microsoft.Extensions.Telemetry.Metrics.HistogramAttribute";
    internal const string TagNameAttribute = "Microsoft.Extensions.Telemetry.Metrics.TagNameAttribute";
    internal const string MeterClass = "System.Diagnostics.Metrics.Meter";

    internal static SymbolHolder? LoadSymbols(Compilation compilation)
    {
        var meterClassSymbol = compilation.GetTypeByMetadataName(MeterClass);
        var counterAttribute = compilation.GetTypeByMetadataName(CounterAttribute);
        var histogramAttribute = compilation.GetTypeByMetadataName(HistogramAttribute);

        if (meterClassSymbol == null ||
            counterAttribute == null ||
            histogramAttribute == null)
        {
            // nothing to do if these types aren't available
            return null;
        }

        var counterTAttribute = compilation.GetTypeByMetadataName(CounterTAttribute);
        var histogramTAttribute = compilation.GetTypeByMetadataName(HistogramTAttribute);
        var gaugeAttribute = compilation.GetTypeByMetadataName(GaugeAttribute);
        var tagNameAttribute = compilation.GetTypeByMetadataName(TagNameAttribute);
        var longType = compilation.GetSpecialType(SpecialType.System_Int64);

        return new(
            meterClassSymbol,
            counterAttribute,
            counterTAttribute,
            histogramAttribute,
            histogramTAttribute,
            gaugeAttribute,
            longType,
            tagNameAttribute);
    }
}
