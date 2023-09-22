// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Diagnostics.Latency;

/// <summary>
/// Extensions to add a no-op latency context.
/// </summary>
public static class NullLatencyContextExtensions
{
    /// <summary>
    /// Adds a no-op latency context to a dependency injection container.
    /// </summary>
    /// <param name="services">The dependency injection container to add the context to.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddNullLatencyContext(this IServiceCollection services);
}
