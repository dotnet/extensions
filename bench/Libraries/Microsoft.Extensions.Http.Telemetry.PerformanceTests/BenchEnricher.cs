﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Bench;

internal sealed class BenchEnricher : IHttpClientLogEnricher
{
    private const string Key = "Performance in R9";
    private const string Value = "is paramount.";

    public void Enrich(IEnrichmentTagCollector collector, HttpRequestMessage? request = null,
        HttpResponseMessage? response = null)
    {
        if (request is not null)
        {
            collector.Add(Key, Value);
        }
    }
}
