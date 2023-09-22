// Assembly 'Microsoft.Extensions.Http.Telemetry'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Logging;

namespace Microsoft.Extensions.Http.Latency;

public static class HttpClientLatencyTelemetryExtensions
{
    public static IServiceCollection AddDefaultHttpClientLatencyTelemetry(this IServiceCollection services);
    public static IServiceCollection AddDefaultHttpClientLatencyTelemetry(this IServiceCollection services, IConfigurationSection section);
    public static IServiceCollection AddDefaultHttpClientLatencyTelemetry(this IServiceCollection services, Action<HttpClientLatencyTelemetryOptions> configure);
}
