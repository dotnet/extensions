// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

namespace Microsoft.Extensions.Telemetry.Latency;

public interface ILatencyContextProvider
{
    ILatencyContext CreateContext();
}
