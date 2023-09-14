// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Diagnostics.Latency;

public interface ILatencyDataExporter
{
    Task ExportAsync(LatencyData data, CancellationToken cancellationToken);
}
