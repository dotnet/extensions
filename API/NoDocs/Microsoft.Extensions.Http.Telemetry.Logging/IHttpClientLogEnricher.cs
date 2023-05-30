// Assembly 'Microsoft.Extensions.Http.Telemetry'

using System.Net.Http;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.Extensions.Http.Telemetry.Logging;

public interface IHttpClientLogEnricher
{
    void Enrich(IEnrichmentPropertyBag enrichmentBag, HttpRequestMessage? request = null, HttpResponseMessage? response = null);
}
