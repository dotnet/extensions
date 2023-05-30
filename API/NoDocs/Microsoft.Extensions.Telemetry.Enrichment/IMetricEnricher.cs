// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

namespace Microsoft.Extensions.Telemetry.Enrichment;

public interface IMetricEnricher
{
    void Enrich(IEnrichmentPropertyBag bag);
}
