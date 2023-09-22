// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

namespace Microsoft.Extensions.Diagnostics.Latency;

/// <summary>
/// A factory of latency contexts.
/// </summary>
public interface ILatencyContextProvider
{
    /// <summary>
    /// Creates a new <see cref="T:Microsoft.Extensions.Diagnostics.Latency.ILatencyContext" />.
    /// </summary>
    /// <returns>A new latency context.</returns>
    ILatencyContext CreateContext();
}
