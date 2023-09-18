// Assembly 'Microsoft.AspNetCore.Telemetry.Middleware'

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.Enrichment;

namespace Microsoft.AspNetCore.Telemetry;

public interface IHttpLogEnricher
{
    void Enrich(IEnrichmentTagCollector collector, HttpRequest request, HttpResponse response);
}
