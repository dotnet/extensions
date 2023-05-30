// Assembly 'Microsoft.Extensions.Http.Telemetry'

using System.Net.Http;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.Extensions.Http.Telemetry.Logging;

/// <summary>
/// Interface for implementing log enrichers for HTTP client requests.
/// </summary>
public interface IHttpClientLogEnricher
{
    /// <summary>
    /// Enrich HTTP client request logs.
    /// </summary>
    /// <param name="enrichmentBag">Property bag to add enriched properties to.</param>
    /// <param name="request"><see cref="T:System.Net.Http.HttpRequestMessage" /> object associated with the outgoing HTTP request.</param>
    /// <param name="response"><see cref="T:System.Net.Http.HttpResponseMessage" /> object associated with the outgoing HTTP request.</param>
    void Enrich(IEnrichmentPropertyBag enrichmentBag, HttpRequestMessage? request = null, HttpResponseMessage? response = null);
}
