// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

namespace Microsoft.Extensions.Diagnostics.Enrichment;

public interface IEnrichmentTagCollector
{
    void Add(string tagName, object tagValue);
}
