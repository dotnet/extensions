// Assembly 'Microsoft.Extensions.Telemetry'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Telemetry.Latency;

public static class LatencyContextExtensions
{
    public static IServiceCollection AddLatencyContext(this IServiceCollection services);
    public static IServiceCollection AddLatencyContext(this IServiceCollection services, Action<LatencyContextOptions> configure);
    public static IServiceCollection AddLatencyContext(this IServiceCollection services, IConfigurationSection section);
}
