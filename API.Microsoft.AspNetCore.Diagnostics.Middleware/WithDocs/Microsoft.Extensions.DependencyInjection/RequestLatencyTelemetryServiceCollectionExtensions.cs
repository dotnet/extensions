// Assembly 'Microsoft.AspNetCore.Diagnostics.Middleware'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Diagnostics.Latency;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions to register the request latency telemetry middleware.
/// </summary>
public static class RequestLatencyTelemetryServiceCollectionExtensions
{
    /// <summary>
    /// Adds all request checkpointing services.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service to.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddRequestCheckpoint(this IServiceCollection services);

    /// <summary>
    /// Adds request latency telemetry middleware to the specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add to.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddRequestLatencyTelemetry(this IServiceCollection services);

    /// <summary>
    /// Adds request latency telemetry middleware to the specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add to.</param>
    /// <param name="configure">Configuration of <see cref="T:Microsoft.AspNetCore.Diagnostics.Latency.RequestLatencyTelemetryOptions" />.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException">Either <paramref name="services" /> or <paramref name="configure" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddRequestLatencyTelemetry(this IServiceCollection services, Action<RequestLatencyTelemetryOptions> configure);

    /// <summary>
    /// Adds request latency telemetry middleware to the specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add to.</param>
    /// <param name="section">Configuration of <see cref="T:Microsoft.AspNetCore.Diagnostics.Latency.RequestLatencyTelemetryOptions" />.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException">Either <paramref name="services" /> or <paramref name="section" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddRequestLatencyTelemetry(this IServiceCollection services, IConfigurationSection section);
}
