// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System.Diagnostics;

namespace Microsoft.Extensions.Telemetry.Enrichment;

/// <summary>
/// Augments tracing state with additional tags.
/// </summary>
public interface ITraceEnricher
{
    /// <summary>
    /// Adds tags to a tracing activity.
    /// </summary>
    /// <param name="activity">The activity to add the tags to.</param>
    void Enrich(Activity activity);

    /// <summary>
    /// Adds tags to the start event of a tracing activity.
    /// </summary>
    /// <param name="activity">The activity to add the tags to.</param>
    void EnrichOnActivityStart(Activity activity);
}
