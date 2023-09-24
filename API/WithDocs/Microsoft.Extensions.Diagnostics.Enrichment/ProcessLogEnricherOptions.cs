// Assembly 'Microsoft.Extensions.Diagnostics.Extra'

using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Diagnostics.Enrichment;

/// <summary>
/// Options for the process enricher.
/// </summary>
public class ProcessLogEnricherOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether current process ID is used for log enrichment.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true" />.
    /// </value>
    public bool ProcessId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether current thread ID is used for log enrichment.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false" />.
    /// </value>
    public bool ThreadId { get; set; }

    public ProcessLogEnricherOptions();
}
