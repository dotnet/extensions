// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Telemetry.Latency;

/// <summary>
/// Abstraction that is used to export latency data.
/// </summary>
/// <remarks>This is called when latency context is frozen to export the context's data.</remarks>
public interface ILatencyDataExporter
{
    /// <summary>
    /// Function called to export latency data.
    /// </summary>
    /// <param name="data">A latency context's latency data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the export operation.</returns>
    Task ExportAsync(LatencyData data, CancellationToken cancellationToken);
}
