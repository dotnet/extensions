// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Diagnostics.Enrichment;

[Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public interface IStaticLogEnricher
{
    void Enrich(IEnrichmentTagCollector collector);
}
