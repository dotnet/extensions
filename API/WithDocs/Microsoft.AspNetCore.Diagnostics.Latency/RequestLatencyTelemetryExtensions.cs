// Assembly 'Microsoft.AspNetCore.Telemetry.Middleware'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Diagnostics.Latency;

/// <summary>
/// Extensions for registering the request latency telemetry middleware.
/// </summary>
public static class RequestLatencyTelemetryExtensions
{
    /// <summary>
    /// Adds request latency telemetry middleware to the specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add to.</param>
    /// <returns>Provided service collection with request latency telemetry middleware added.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddRequestLatencyTelemetry(this IServiceCollection services);

    /// <summary>
    /// Adds request latency telemetry middleware to the specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add to.</param>
    /// <param name="configure">Configuration of <see cref="T:Microsoft.AspNetCore.Diagnostics.Latency.RequestLatencyTelemetryOptions" />.</param>
    /// <returns>Provided service collection with request latency telemetry middleware added.</returns>
    /// <exception cref="T:System.ArgumentNullException">Either <paramref name="services" /> or <paramref name="configure" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddRequestLatencyTelemetry(this IServiceCollection services, Action<RequestLatencyTelemetryOptions> configure);

    /// <summary>
    /// Adds request latency telemetry middleware to the specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add to.</param>
    /// <param name="section">Configuration of <see cref="T:Microsoft.AspNetCore.Diagnostics.Latency.RequestLatencyTelemetryOptions" />.</param>
    /// <returns>Provided service collection with request latency telemetry middleware added.</returns>
    /// <exception cref="T:System.ArgumentNullException">Either <paramref name="services" /> or <paramref name="section" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddRequestLatencyTelemetry(this IServiceCollection services, IConfigurationSection section);

    /// <summary>
    /// Adds the request latency telemetry middleware to <see cref="T:Microsoft.AspNetCore.Builder.IApplicationBuilder" /> request execution pipeline.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.AspNetCore.Builder.IApplicationBuilder" />.</param>
    /// <returns>The <see cref="T:Microsoft.AspNetCore.Builder.IApplicationBuilder" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    public static IApplicationBuilder UseRequestLatencyTelemetry(this IApplicationBuilder builder);
}
