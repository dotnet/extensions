// Assembly 'Microsoft.Extensions.Telemetry'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Diagnostics.Latency;

/// <summary>
/// Extensions to add latency context.
/// </summary>
public static class LatencyContextExtensions
{
    /// <summary>
    /// Adds latency context.
    /// </summary>
    /// <param name="services">The dependency injection container.</param>
    /// <returns>The provided service collection with <see cref="T:Microsoft.Extensions.Diagnostics.Latency.ILatencyContext" /> added.</returns>
    public static IServiceCollection AddLatencyContext(this IServiceCollection services);

    /// <summary>
    /// Adds latency context.
    /// </summary>
    /// <param name="services">The dependency injection container.</param>
    /// <param name="configure">The <see cref="T:Microsoft.Extensions.Diagnostics.Latency.LatencyContextOptions" /> configuration delegate.</param>
    /// <returns>The provided service collection with <see cref="T:Microsoft.Extensions.Diagnostics.Latency.Internal.LatencyContextProvider" /> added.</returns>
    public static IServiceCollection AddLatencyContext(this IServiceCollection services, Action<LatencyContextOptions> configure);

    /// <summary>
    /// Adds latency context.
    /// </summary>
    /// <param name="services">The dependency injection container.</param>
    /// <param name="section">The configuration of <see cref="T:Microsoft.Extensions.Diagnostics.Latency.LatencyContextOptions" />.</param>
    /// <returns>The provided service collection with <see cref="T:Microsoft.Extensions.Diagnostics.Latency.Internal.LatencyContextProvider" /> added.</returns>
    public static IServiceCollection AddLatencyContext(this IServiceCollection services, IConfigurationSection section);
}
