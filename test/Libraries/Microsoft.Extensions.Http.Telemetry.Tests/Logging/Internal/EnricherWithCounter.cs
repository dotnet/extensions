// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Test.Internal;

internal class EnricherWithCounter : IHttpClientLogEnricher
{
    public int TimesCalled;

    public void Enrich(IEnrichmentPropertyBag enrichmentBag, HttpRequestMessage request, HttpResponseMessage? response = null, Exception? exception = null)
        => TimesCalled++;
}
