// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

namespace Microsoft.Extensions.Diagnostics.Latency;

public interface ILatencyContextProvider
{
    ILatencyContext CreateContext();
}
