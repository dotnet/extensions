// Assembly 'Microsoft.Extensions.Http.Telemetry'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Telemetry.Logging;

namespace Microsoft.Extensions.Http.Telemetry.Latency;

/// <summary>
/// Extension methods to add http client latency telemetry.
/// </summary>
public static class HttpClientLatencyTelemetryExtensions
{
    /// <summary>
    /// Adds a <see cref="T:System.Net.Http.DelegatingHandler" /> to collect latency information and enrich outgoing request log for all http clients.
    /// </summary>
    /// <remarks>
    /// This extension configures latency information collection globally for all http clients.
    /// </remarks>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.</param>
    /// <returns>
    /// <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> instance for chaining.
    /// </returns>
    public static IServiceCollection AddDefaultHttpClientLatencyTelemetry(this IServiceCollection services);

    /// <summary>
    /// Adds a <see cref="T:System.Net.Http.DelegatingHandler" /> to collect latency information and enrich outgoing request log for all http clients.
    /// </summary>
    /// <remarks>
    /// This extension configures outgoing request logs auto collection globally for all http clients.
    /// </remarks>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.</param>
    /// <param name="section">The <see cref="T:Microsoft.Extensions.Configuration.IConfigurationSection" /> to use for configuring <see cref="T:Microsoft.Extensions.Http.Telemetry.Latency.HttpClientLatencyTelemetryOptions" />.</param>
    /// <returns>
    /// <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> instance for chaining.
    /// </returns>
    public static IServiceCollection AddDefaultHttpClientLatencyTelemetry(this IServiceCollection services, IConfigurationSection section);

    /// <summary>
    /// Adds a <see cref="T:System.Net.Http.DelegatingHandler" /> to collect latency information and enrich outgoing request log for all http clients.
    /// </summary>
    /// <remarks>
    /// This extension configures outgoing request logs auto collection globally for all http clients.
    /// </remarks>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.</param>
    /// <param name="configure">The delegate to configure <see cref="T:Microsoft.Extensions.Http.Telemetry.Latency.HttpClientLatencyTelemetryOptions" /> with.</param>
    /// <returns>
    /// <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> instance for chaining.
    /// </returns>
    public static IServiceCollection AddDefaultHttpClientLatencyTelemetry(this IServiceCollection services, Action<HttpClientLatencyTelemetryOptions> configure);
}
