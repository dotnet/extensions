// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

namespace Microsoft.Extensions.Telemetry.Enrichment;

/// <summary>
/// A component that augments log records with additional properties.
/// </summary>
public interface ILogEnricher
{
    /// <summary>
    /// Called to generate properties for a log record.
    /// </summary>
    /// <param name="bag">Where the enricher puts the properties it is producing.</param>
    void Enrich(IEnrichmentPropertyBag bag);
}
