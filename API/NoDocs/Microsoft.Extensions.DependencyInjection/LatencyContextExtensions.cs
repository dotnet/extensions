// Assembly 'Microsoft.Extensions.Diagnostics.Extra'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.Latency;

namespace Microsoft.Extensions.DependencyInjection;

public static class LatencyContextExtensions
{
    public static IServiceCollection AddLatencyContext(this IServiceCollection services);
    public static IServiceCollection AddLatencyContext(this IServiceCollection services, Action<LatencyContextOptions> configure);
    public static IServiceCollection AddLatencyContext(this IServiceCollection services, IConfigurationSection section);
}
