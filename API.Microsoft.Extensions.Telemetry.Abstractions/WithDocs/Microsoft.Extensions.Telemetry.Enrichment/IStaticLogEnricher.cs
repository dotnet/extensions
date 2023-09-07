// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Telemetry.Enrichment;

/// <summary>
/// A component that augments log records with additional properties which are unchanging over the life of the object.
/// </summary>
[Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public interface IStaticLogEnricher
{
    /// <summary>
    /// Called to collect tags for a log record.
    /// </summary>
    /// <param name="collector">Where the enricher puts the tags it is producing.</param>
    void Enrich(IEnrichmentTagCollector collector);
}
