// Assembly 'Microsoft.Extensions.Diagnostics.ExtraAbstractions'

namespace Microsoft.Extensions.Diagnostics.Enrichment;

public interface IStaticLogEnricher
{
    void Enrich(IEnrichmentTagCollector collector);
}
