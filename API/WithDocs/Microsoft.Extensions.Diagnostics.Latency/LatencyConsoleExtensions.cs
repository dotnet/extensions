// Assembly 'Microsoft.Extensions.Telemetry'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Diagnostics.Latency;

/// <summary>
/// Extensions to add console latency data exporter.
/// </summary>
public static class LatencyConsoleExtensions
{
    /// <summary>
    /// Add latency data exporter for the console.
    /// </summary>
    /// <param name="services">Dependency injection container.</param>
    /// <returns>Provided service collection with <see cref="T:Microsoft.Extensions.Diagnostics.Latency.Internal.LatencyConsoleExporter" /> added.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddConsoleLatencyDataExporter(this IServiceCollection services);

    /// <summary>
    /// Add latency data exporter for the console.
    /// </summary>
    /// <param name="services">Dependency injection container.</param>
    /// <param name="configure"><see cref="T:Microsoft.Extensions.Diagnostics.Latency.LatencyConsoleOptions" /> configuration delegate.</param>
    /// <returns>Provided service collection with <see cref="T:Microsoft.Extensions.Diagnostics.Latency.Internal.LatencyConsoleExporter" /> added.</returns>
    /// <exception cref="T:System.ArgumentNullException">Either <paramref name="services" /> or <paramref name="configure" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddConsoleLatencyDataExporter(this IServiceCollection services, Action<LatencyConsoleOptions> configure);

    /// <summary>
    /// Add latency data exporter for the console.
    /// </summary>
    /// <param name="services">Dependency injection container.</param>
    /// <param name="section">Configuration of <see cref="T:Microsoft.Extensions.Diagnostics.Latency.LatencyConsoleOptions" />.</param>
    /// <returns>Provided service collection with <see cref="T:Microsoft.Extensions.Diagnostics.Latency.Internal.LatencyConsoleExporter" /> added.</returns>
    /// <exception cref="T:System.ArgumentNullException">Either <paramref name="services" /> or <paramref name="section" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddConsoleLatencyDataExporter(this IServiceCollection services, IConfigurationSection section);
}
