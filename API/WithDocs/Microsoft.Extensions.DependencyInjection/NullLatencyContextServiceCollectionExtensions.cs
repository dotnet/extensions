// Assembly 'Microsoft.Extensions.Diagnostics.ExtraAbstractions'

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions to add a no-op latency context.
/// </summary>
public static class NullLatencyContextServiceCollectionExtensions
{
    /// <summary>
    /// Adds a no-op latency context to a dependency injection container.
    /// </summary>
    /// <param name="services">The dependency injection container to add the context to.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddNullLatencyContext(this IServiceCollection services);
}
