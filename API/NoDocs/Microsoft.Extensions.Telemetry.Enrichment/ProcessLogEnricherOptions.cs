// Assembly 'Microsoft.Extensions.Telemetry'

using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Enrichment;

public class ProcessLogEnricherOptions
{
    public bool ProcessId { get; set; }
    public bool ThreadId { get; set; }
    public ProcessLogEnricherOptions();
}
