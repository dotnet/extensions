// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Metrics;

namespace Microsoft.AspNetCore.HeaderParsing;

internal sealed class HeaderParsingMetrics : IDisposable
{
    private const string MeterName = "Microsoft.AspNetCore.HeaderParsing";

    private readonly Meter _meter;
    private readonly ParsingErrorCounter _parsingErrorCounter;
    private readonly CacheAccessCounter _cacheAccessCounter;

    public HeaderParsingMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);
        _parsingErrorCounter = Metric.CreateParsingErrorCounter(_meter);
        _cacheAccessCounter = Metric.CreateCacheAccessCounter(_meter);
    }

    public void CacheAccessed(string headerName, string type)
        => _cacheAccessCounter.Add(1, headerName, type);

    public void ParsingErrorOccurred(string headerName, string? kind)
        => _parsingErrorCounter.Add(1, headerName, kind);

    public void Dispose()
        => _meter.Dispose();
}
