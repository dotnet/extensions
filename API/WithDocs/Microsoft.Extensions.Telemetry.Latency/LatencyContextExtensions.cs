// Assembly 'Microsoft.Extensions.Telemetry'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Telemetry.Latency;

/// <summary>
/// Extensions to add latency context.
/// </summary>
public static class LatencyContextExtensions
{
    /// <summary>
    /// Add latency context.
    /// </summary>
    /// <param name="services">Dependency injection container.</param>
    /// <returns>Provided service collection with <see cref="T:Microsoft.Extensions.Telemetry.Latency.ILatencyContext" /> added.</returns>
    public static IServiceCollection AddLatencyContext(this IServiceCollection services);

    /// <summary>
    /// Add latency context.
    /// </summary>
    /// <param name="services">Dependency injection container.</param>
    /// <param name="configure"><see cref="T:Microsoft.Extensions.Telemetry.Latency.LatencyContextOptions" /> configuration delegate.</param>
    /// <returns>Provided service collection with <see cref="T:Microsoft.Extensions.Telemetry.Latency.Internal.LatencyContextProvider" /> added.</returns>
    public static IServiceCollection AddLatencyContext(this IServiceCollection services, Action<LatencyContextOptions> configure);

    /// <summary>
    /// Add latency context.
    /// </summary>
    /// <param name="services">Dependency injection container.</param>
    /// <param name="section">Configuration of <see cref="T:Microsoft.Extensions.Telemetry.Latency.LatencyContextOptions" />.</param>
    /// <returns>Provided service collection with <see cref="T:Microsoft.Extensions.Telemetry.Latency.Internal.LatencyContextProvider" /> added.</returns>
    public static IServiceCollection AddLatencyContext(this IServiceCollection services, IConfigurationSection section);
}
