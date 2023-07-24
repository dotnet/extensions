// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Gen.Metering;

internal sealed record class SymbolHolder(
    INamedTypeSymbol MeterSymbol,
    INamedTypeSymbol CounterAttribute,
    INamedTypeSymbol? CounterOfTAttribute,
    INamedTypeSymbol HistogramAttribute,
    INamedTypeSymbol? HistogramOfTAttribute,
    INamedTypeSymbol? GaugeAttribute,
    INamedTypeSymbol LongTypeSymbol,
    INamedTypeSymbol? DimensionAttribute);
