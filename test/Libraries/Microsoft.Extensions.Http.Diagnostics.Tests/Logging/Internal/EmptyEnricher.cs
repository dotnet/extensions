// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.Diagnostics.Enrichment;

namespace Microsoft.Extensions.Http.Logging.Test.Internal;

internal class EmptyEnricher : IHttpClientLogEnricher
{
    public void Enrich(IEnrichmentTagCollector collector, HttpRequestMessage request, HttpResponseMessage? response, Exception? exception)
    {
        // intentionally left empty.
    }
}
