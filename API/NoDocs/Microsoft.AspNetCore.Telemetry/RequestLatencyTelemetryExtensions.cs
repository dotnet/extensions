// Assembly 'Microsoft.AspNetCore.Telemetry.Middleware'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Telemetry;

public static class RequestLatencyTelemetryExtensions
{
    public static IServiceCollection AddRequestLatencyTelemetry(this IServiceCollection services);
    public static IServiceCollection AddRequestLatencyTelemetry(this IServiceCollection services, Action<RequestLatencyTelemetryOptions> configure);
    public static IServiceCollection AddRequestLatencyTelemetry(this IServiceCollection services, IConfigurationSection section);
    public static IApplicationBuilder UseRequestLatencyTelemetry(this IApplicationBuilder builder);
}
