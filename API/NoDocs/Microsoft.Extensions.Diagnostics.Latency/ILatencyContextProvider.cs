// Assembly 'Microsoft.Extensions.Diagnostics.ExtraAbstractions'

namespace Microsoft.Extensions.Diagnostics.Latency;

public interface ILatencyContextProvider
{
    ILatencyContext CreateContext();
}
