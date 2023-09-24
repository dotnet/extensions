// Assembly 'Microsoft.Extensions.Diagnostics.Extra'

using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Diagnostics.Enrichment;

public class ProcessLogEnricherOptions
{
    public bool ProcessId { get; set; }
    public bool ThreadId { get; set; }
    public ProcessLogEnricherOptions();
}
