// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Gen.Metering;

internal static class SymbolLoader
{
    internal const string CounterTAttribute = "Microsoft.Extensions.Telemetry.Metering.CounterAttribute`1";
    internal const string HistogramTAttribute = "Microsoft.Extensions.Telemetry.Metering.HistogramAttribute`1";
    internal const string CounterAttribute = "Microsoft.Extensions.Telemetry.Metering.CounterAttribute";
    internal const string HistogramAttribute = "Microsoft.Extensions.Telemetry.Metering.HistogramAttribute";
    internal const string DimensionAttribute = "Microsoft.Extensions.Telemetry.Metering.DimensionAttribute";
    internal const string MeterClass = "System.Diagnostics.Metrics.Meter";
    private const string MeterInterface = "Microsoft.Extensions.Telemetry.Metering.IMeter";

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
        var dimensionAttribute = compilation.GetTypeByMetadataName(DimensionAttribute);
        var longType = compilation.GetSpecialType(SpecialType.System_Int64);
        var meterInterface = compilation.GetTypeByMetadataName(MeterInterface);

        return new(
            meterClassSymbol,
            counterAttribute,
            counterTAttribute,
            histogramAttribute,
            histogramTAttribute,
            longType,
            dimensionAttribute,
            meterInterface);
    }
}
