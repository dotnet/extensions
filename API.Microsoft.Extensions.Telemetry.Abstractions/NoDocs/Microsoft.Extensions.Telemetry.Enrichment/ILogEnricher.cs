// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

namespace Microsoft.Extensions.Telemetry.Enrichment;

public interface ILogEnricher
{
    void Enrich(IEnrichmentTagCollector collector);
}
