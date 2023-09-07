// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Telemetry.Latency;

public interface ILatencyDataExporter
{
    Task ExportAsync(LatencyData data, CancellationToken cancellationToken);
}
