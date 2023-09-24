// Assembly 'Microsoft.Extensions.Http.Diagnostics'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http.Latency;
using Microsoft.Extensions.Http.Logging;

namespace Microsoft.Extensions.DependencyInjection;

public static class HttpClientLatencyTelemetryExtensions
{
    public static IServiceCollection AddDefaultHttpClientLatencyTelemetry(this IServiceCollection services);
    public static IServiceCollection AddDefaultHttpClientLatencyTelemetry(this IServiceCollection services, IConfigurationSection section);
    public static IServiceCollection AddDefaultHttpClientLatencyTelemetry(this IServiceCollection services, Action<HttpClientLatencyTelemetryOptions> configure);
}
