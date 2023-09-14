// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Diagnostics.Latency;

/// <summary>
/// Extensions to configure a latency context.
/// </summary>
public static class LatencyRegistryExtensions
{
    /// <summary>
    /// Registers a set of checkpoint names for a latency context.
    /// </summary>
    /// <param name="services">The dependency injection container to add the names to.</param>
    /// <param name="names">Set of checkpoint names.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> or <paramref name="names" /> is <see langword="null" />.</exception>
    public static IServiceCollection RegisterCheckpointNames(this IServiceCollection services, params string[] names);

    /// <summary>
    /// Registers a set of measure names for a latency context.
    /// </summary>
    /// <param name="services">The dependency injection container to add the names to.</param>
    /// <param name="names">Set of measure names.</param>
    /// <returns>Provided service collection.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> or <paramref name="names" /> is <see langword="null" />.</exception>
    public static IServiceCollection RegisterMeasureNames(this IServiceCollection services, params string[] names);

    /// <summary>
    /// Registers a set of tag names for a latency context.
    /// </summary>
    /// <param name="services">The dependency injection container to add the names to.</param>
    /// <param name="names">Set of tag names.</param>
    /// <returns>Provided service collection.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> or <paramref name="names" /> is <see langword="null" />.</exception>
    public static IServiceCollection RegisterTagNames(this IServiceCollection services, params string[] names);
}
