// Assembly 'Microsoft.Extensions.Http.Diagnostics'

using System;
using System.Net.Http;
using Microsoft.Extensions.Diagnostics.Enrichment;

namespace Microsoft.Extensions.Http.Logging;

public interface IHttpClientLogEnricher
{
    void Enrich(IEnrichmentTagCollector collector, HttpRequestMessage request, HttpResponseMessage? response, Exception? exception);
}
