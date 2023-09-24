// Assembly 'Microsoft.Extensions.Diagnostics.ExtraAbstractions'

namespace Microsoft.Extensions.Diagnostics.Enrichment;

public interface ILogEnricher
{
    void Enrich(IEnrichmentTagCollector collector);
}
