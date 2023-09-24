// Assembly 'Microsoft.AspNetCore.Diagnostics.Middleware'

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.Enrichment;

namespace Microsoft.AspNetCore.Diagnostics.Logging;

public interface IHttpLogEnricher
{
    void Enrich(IEnrichmentTagCollector collector, HttpRequest request, HttpResponse response);
}
