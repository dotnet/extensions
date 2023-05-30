// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System.Diagnostics;

namespace Microsoft.Extensions.Telemetry.Enrichment;

public interface ITraceEnricher
{
    void Enrich(Activity activity);
    void EnrichOnActivityStart(Activity activity);
}
