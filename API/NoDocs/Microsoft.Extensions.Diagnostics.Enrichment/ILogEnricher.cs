// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

namespace Microsoft.Extensions.Diagnostics.Enrichment;

public interface ILogEnricher
{
    void Enrich(IEnrichmentTagCollector collector);
}
