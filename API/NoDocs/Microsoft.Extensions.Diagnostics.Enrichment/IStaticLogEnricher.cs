// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

namespace Microsoft.Extensions.Diagnostics.Enrichment;

public interface IStaticLogEnricher
{
    void Enrich(IEnrichmentTagCollector collector);
}
