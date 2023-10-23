// Assembly 'Microsoft.AspNetCore.Diagnostics.Middleware'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Diagnostics.Latency;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class RequestLatencyTelemetryServiceCollectionExtensions
{
    public static IServiceCollection AddRequestCheckpoint(this IServiceCollection services);
    public static IServiceCollection AddRequestLatencyTelemetry(this IServiceCollection services);
    public static IServiceCollection AddRequestLatencyTelemetry(this IServiceCollection services, Action<RequestLatencyTelemetryOptions> configure);
    public static IServiceCollection AddRequestLatencyTelemetry(this IServiceCollection services, IConfigurationSection section);
}
