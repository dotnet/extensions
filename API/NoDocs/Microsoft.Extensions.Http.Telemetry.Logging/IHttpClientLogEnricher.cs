// Assembly 'Microsoft.Extensions.Http.Telemetry'

using System;
using System.Net.Http;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.Extensions.Http.Telemetry.Logging;

public interface IHttpClientLogEnricher
{
    void Enrich(IEnrichmentTagCollector collector, HttpRequestMessage request, HttpResponseMessage? response, Exception? exception);
}
