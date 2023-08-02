// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Telemetry.Enrichment;
using Microsoft.Extensions.Telemetry.Logging;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Test.Internal;

internal class TestEnricher : IHttpClientLogEnricher
{
    internal readonly KeyValuePair<string, object?> KvpRequest = new("test key request", "test value");
    internal readonly KeyValuePair<string, object?> KvpResponse = new("test key response", "test value");

    public LoggerMessageState EnrichmentBag { get; }

    public TestEnricher()
    {
        EnrichmentBag = new();
        var index = EnrichmentBag.ReservePropertySpace(2);
        EnrichmentBag.PropertyArray[index++] = KvpRequest;
        EnrichmentBag.PropertyArray[index++] = KvpResponse;
    }

    public void Enrich(IEnrichmentPropertyBag enrichmentBag, HttpRequestMessage request, HttpResponseMessage? response = null, Exception? exception = null)
    {
        if (request is not null)
        {
            enrichmentBag.Add(KvpRequest.Key, KvpRequest.Value!);
        }

        if (response is not null)
        {
            enrichmentBag.Add(KvpResponse.Key, KvpResponse.Value!);
        }
    }
}
