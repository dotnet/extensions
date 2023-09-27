// Assembly 'Microsoft.Extensions.Http.Diagnostics'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http.Latency;
using Microsoft.Extensions.Http.Logging;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions to add http client latency telemetry.
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
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddHttpClientLatencyTelemetry(this IServiceCollection services);

    /// <summary>
    /// Adds a <see cref="T:System.Net.Http.DelegatingHandler" /> to collect latency information and enrich outgoing request log for all http clients.
    /// </summary>
    /// <remarks>
    /// This extension configures outgoing request logs auto collection globally for all http clients.
    /// </remarks>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.</param>
    /// <param name="section">The <see cref="T:Microsoft.Extensions.Configuration.IConfigurationSection" /> to use for configuring <see cref="T:Microsoft.Extensions.Http.Latency.HttpClientLatencyTelemetryOptions" />.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddHttpClientLatencyTelemetry(this IServiceCollection services, IConfigurationSection section);

    /// <summary>
    /// Adds a <see cref="T:System.Net.Http.DelegatingHandler" /> to collect latency information and enrich outgoing request log for all http clients.
    /// </summary>
    /// <remarks>
    /// This extension configures outgoing request logs auto collection globally for all http clients.
    /// </remarks>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.</param>
    /// <param name="configure">The delegate to configure <see cref="T:Microsoft.Extensions.Http.Latency.HttpClientLatencyTelemetryOptions" /> with.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddHttpClientLatencyTelemetry(this IServiceCollection services, Action<HttpClientLatencyTelemetryOptions> configure);
}
