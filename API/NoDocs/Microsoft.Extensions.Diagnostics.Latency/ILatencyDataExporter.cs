// Assembly 'Microsoft.Extensions.Diagnostics.ExtraAbstractions'

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Diagnostics.Latency;

public interface ILatencyDataExporter
{
    Task ExportAsync(LatencyData data, CancellationToken cancellationToken);
}
