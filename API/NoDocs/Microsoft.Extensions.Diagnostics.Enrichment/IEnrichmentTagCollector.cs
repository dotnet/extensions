// Assembly 'Microsoft.Extensions.Diagnostics.ExtraAbstractions'

namespace Microsoft.Extensions.Diagnostics.Enrichment;

public interface IEnrichmentTagCollector
{
    void Add(string tagName, object tagValue);
}
