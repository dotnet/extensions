// Assembly 'Microsoft.AspNetCore.Diagnostics.Middleware'

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.Enrichment;

namespace Microsoft.AspNetCore.Diagnostics.Logging;

/// <summary>
/// Interface for implementing log enrichers for incoming HTTP requests.
/// </summary>
[Experimental("EXTEXP0013", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public interface IHttpLogEnricher
{
    /// <summary>
    /// Enrich logs.
    /// </summary>
    /// <param name="collector">Tag collector to add tags to.</param>
    /// <param name="httpContext"><see cref="T:Microsoft.AspNetCore.Http.HttpContext" /> object associated with the incoming HTTP request.</param>
    void Enrich(IEnrichmentTagCollector collector, HttpContext httpContext);
}
