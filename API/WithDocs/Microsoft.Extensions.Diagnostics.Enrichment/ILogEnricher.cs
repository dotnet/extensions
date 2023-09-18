// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

namespace Microsoft.Extensions.Diagnostics.Enrichment;

/// <summary>
/// Augments log records with additional properties.
/// </summary>
public interface ILogEnricher
{
    /// <summary>
    /// Collects tags for a log record.
    /// </summary>
    /// <param name="collector">Where the enricher puts the tags it produces.</param>
    void Enrich(IEnrichmentTagCollector collector);
}
