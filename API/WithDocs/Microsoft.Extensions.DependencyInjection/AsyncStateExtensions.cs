// Assembly 'Microsoft.Extensions.AsyncState'

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions to manipulate async state.
/// </summary>
public static class AsyncStateExtensions
{
    /// <summary>
    /// Adds default implementations for <see cref="T:Microsoft.Extensions.AsyncState.IAsyncState" />, <see cref="T:Microsoft.Extensions.AsyncState.IAsyncContext`1" />, and <see cref="T:Microsoft.Extensions.AsyncState.IAsyncLocalContext`1" /> services.
    /// </summary>
    /// <param name="services">The dependency injection container to add the implementations to.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddAsyncState(this IServiceCollection services);
}
