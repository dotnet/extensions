// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Telemetry.Latency;

public static class NullLatencyContextExtensions
{
    public static IServiceCollection AddNullLatencyContext(this IServiceCollection services);
}
