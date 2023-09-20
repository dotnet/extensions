// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;

namespace Microsoft.AspNetCore.HeaderParsing;

internal sealed class HeaderParsingMetrics
{
    private const string MeterName = "Microsoft.AspNetCore.HeaderParsing";

    public HeaderParsingMetrics(IMeterFactory meterFactory)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        // We don't dispose the meter because IMeterFactory handles that
        // An issue on analyzer side: https://github.com/dotnet/roslyn-analyzers/issues/6912
        // Related documentation: https://github.com/dotnet/docs/pull/37170
        var meter = meterFactory.Create(MeterName);
#pragma warning restore CA2000 // Dispose objects before losing scope

        ParsingErrorCounter = Metric.CreateParsingErrorCounter(meter);
        CacheAccessCounter = Metric.CreateCacheAccessCounter(meter);
    }

    public ParsingErrorCounter ParsingErrorCounter { get; }

    public CacheAccessCounter CacheAccessCounter { get; }
}
