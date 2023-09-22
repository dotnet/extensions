// Assembly 'Microsoft.Extensions.Telemetry'

using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Diagnostics.Latency;

/// <summary>
/// Options for LatencyContext.
/// </summary>
public class LatencyContextOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether an exception is thrown when using unregistered names.
    /// </summary>
    /// <value>
    /// <see langword="true" /> if the ILatencyContext APIs throws when using unregistered names. <see langword="false" /> to no-op. The default value is <see langword="false" />.
    /// </value>
    public bool ThrowOnUnregisteredNames { get; set; }

    public LatencyContextOptions();
}
