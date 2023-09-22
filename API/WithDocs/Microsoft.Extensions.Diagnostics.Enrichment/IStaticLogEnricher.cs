// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

namespace Microsoft.Extensions.Diagnostics.Enrichment;

/// <summary>
/// A component that augments log records with additional properties which are unchanging over the life of the object.
/// </summary>
public interface IStaticLogEnricher
{
    /// <summary>
    /// Called to collect tags for a log record.
    /// </summary>
    /// <param name="collector">Where the enricher puts the tags it is producing.</param>
    void Enrich(IEnrichmentTagCollector collector);
}
