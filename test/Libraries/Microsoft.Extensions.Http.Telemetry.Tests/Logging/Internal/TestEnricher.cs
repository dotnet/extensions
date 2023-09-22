// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Diagnostics.Enrichment;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Http.Logging.Test.Internal;

internal class TestEnricher : IHttpClientLogEnricher
{
    internal readonly KeyValuePair<string, object?> KvpRequest = new("test key request", "test value");
    internal readonly KeyValuePair<string, object?> KvpResponse = new("test key response", "test value");
    private readonly bool _throwOnEnrich;

    public LoggerMessageState EnrichmentBag { get; }

    public TestEnricher(bool throwOnEnrich = false)
    {
        EnrichmentBag = new();
        var index = EnrichmentBag.ReserveTagSpace(2);
        EnrichmentBag.TagArray[index++] = KvpRequest;
        EnrichmentBag.TagArray[index++] = KvpResponse;
        _throwOnEnrich = throwOnEnrich;
    }

    public void Enrich(IEnrichmentTagCollector tagCollector, HttpRequestMessage request, HttpResponseMessage? response, Exception? exception)
    {
        if (_throwOnEnrich)
        {
            throw new NotSupportedException("Synthetic exception from enricher");
        }

        if (request is not null)
        {
            tagCollector.Add(KvpRequest.Key, KvpRequest.Value!);
        }

        if (response is not null)
        {
            tagCollector.Add(KvpResponse.Key, KvpResponse.Value!);
        }
    }
}
