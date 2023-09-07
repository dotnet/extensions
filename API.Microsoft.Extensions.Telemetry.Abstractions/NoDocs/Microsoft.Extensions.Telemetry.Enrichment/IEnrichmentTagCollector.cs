// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

namespace Microsoft.Extensions.Telemetry.Enrichment;

public interface IEnrichmentTagCollector
{
    void Add(string tagName, object tagValue);
}
