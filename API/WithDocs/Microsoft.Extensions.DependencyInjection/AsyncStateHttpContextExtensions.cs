// Assembly 'Microsoft.AspNetCore.AsyncState'

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to add the async state feature with the HttpContext lifetime to a dependency injection container.
/// </summary>
public static class AsyncStateHttpContextExtensions
{
    /// <summary>
    /// Adds default implementations for <see cref="T:Microsoft.Extensions.AsyncState.IAsyncState" />, <see cref="T:Microsoft.Extensions.AsyncState.IAsyncContext`1" />, and <see cref="T:Microsoft.Extensions.AsyncState.IAsyncLocalContext`1" /> services,
    /// scoped to the lifetime of <see cref="T:Microsoft.AspNetCore.Http.HttpContext" /> instances.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service to.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddAsyncStateHttpContext(this IServiceCollection services);
}
