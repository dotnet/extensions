// Assembly 'Microsoft.Extensions.Diagnostics.ExtraAbstractions'

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Diagnostics.Latency;

public static class NullLatencyContextExtensions
{
    public static IServiceCollection AddNullLatencyContext(this IServiceCollection services);
}
