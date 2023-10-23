// Assembly 'Microsoft.AspNetCore.Diagnostics.Middleware'

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.Enrichment;

namespace Microsoft.AspNetCore.Diagnostics.Logging;

[Experimental("EXTEXP0013", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public interface IHttpLogEnricher
{
    void Enrich(IEnrichmentTagCollector collector, HttpContext httpContext);
}
