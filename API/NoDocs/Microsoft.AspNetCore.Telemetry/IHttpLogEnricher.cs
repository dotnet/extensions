// Assembly 'Microsoft.AspNetCore.Telemetry.Middleware'

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.AspNetCore.Telemetry;

public interface IHttpLogEnricher
{
    void Enrich(IEnrichmentPropertyBag enrichmentBag, HttpRequest request, HttpResponse response);
}
