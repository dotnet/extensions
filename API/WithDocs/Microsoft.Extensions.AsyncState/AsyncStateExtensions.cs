// Assembly 'Microsoft.Extensions.AsyncState'

using System;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.AsyncState;

/// <summary>
/// Extension methods to manipulate async state.
/// </summary>
public static class AsyncStateExtensions
{
    /// <summary>
    /// Adds default implementations for <see cref="T:Microsoft.Extensions.AsyncState.IAsyncState" />, <see cref="T:Microsoft.Extensions.AsyncState.IAsyncContext`1" />, and <see cref="T:Microsoft.Extensions.AsyncState.IAsyncLocalContext`1" /> services.
    /// </summary>
    /// <param name="services">The dependency injection container to add the implementations to.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddAsyncStateCore(this IServiceCollection services);

    /// <summary>
    /// Tries to remove the default implementation for <see cref="T:Microsoft.Extensions.AsyncState.IAsyncState" />, <see cref="T:Microsoft.Extensions.AsyncState.IAsyncContext`1" />, and <see cref="T:Microsoft.Extensions.AsyncState.IAsyncLocalContext`1" /> services.
    /// </summary>
    /// <param name="services">The dependency injection container to remove the implementations from.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IServiceCollection TryRemoveAsyncStateCore(this IServiceCollection services);
}
