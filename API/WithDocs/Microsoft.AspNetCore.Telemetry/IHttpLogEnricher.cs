// Assembly 'Microsoft.AspNetCore.Telemetry.Middleware'

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.AspNetCore.Telemetry;

/// <summary>
/// Interface for implementing log enrichers for incoming HTTP requests.
/// </summary>
public interface IHttpLogEnricher
{
    /// <summary>
    /// Enrich logs.
    /// </summary>
    /// <param name="enrichmentBag">Property bag to add enriched properties to.</param>
    /// <param name="request"><see cref="T:Microsoft.AspNetCore.Http.HttpResponse" /> object associated with the incoming HTTP request.</param>
    /// <param name="response"><see cref="T:Microsoft.AspNetCore.Http.HttpResponse" /> object associated with the response to an incoming HTTP request.</param>
    void Enrich(IEnrichmentPropertyBag enrichmentBag, HttpRequest request, HttpResponse response);
}
