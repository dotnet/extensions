// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

namespace Microsoft.Extensions.Telemetry.Latency;

/// <summary>
/// A factory of latency contextts.
/// </summary>
public interface ILatencyContextProvider
{
    /// <summary>
    /// Creates a new <see cref="T:Microsoft.Extensions.Telemetry.Latency.ILatencyContext" />.
    /// </summary>
    /// <returns>A new latency context.</returns>
    ILatencyContext CreateContext();
}
