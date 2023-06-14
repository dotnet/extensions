// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AsyncState;

/// <summary>
/// Extension methods to manipulate async state.
/// </summary>
public static class AsyncStateExtensions
{
    /// <summary>
    /// Adds default implementations for <see cref="IAsyncState"/>, <see cref="IAsyncContext{T}"/>, and <see cref="IAsyncLocalContext{T}"/> services.
    /// </summary>
    /// <param name="services">The dependency injection container to add the implementations to.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null" />.</exception>
    public static IServiceCollection AddAsyncStateCore(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        services.TryAddSingleton(typeof(IAsyncContext<>), typeof(AsyncContext<>));
        services.TryAddActivatedSingleton<IAsyncState, AsyncState>();
        services.TryAddSingleton(typeof(IAsyncLocalContext<>), typeof(AsyncContext<>));

        return services;
    }

    /// <summary>
    /// Tries to remove the default implementation for <see cref="IAsyncState"/>, <see cref="IAsyncContext{T}"/>, and <see cref="IAsyncLocalContext{T}"/> services.
    /// </summary>
    /// <param name="services">The dependency injection container to remove the implementations from.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null" />.</exception>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IServiceCollection TryRemoveAsyncStateCore(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        services.TryRemoveSingleton(typeof(IAsyncContext<>), typeof(AsyncContext<>));

        return services;
    }

    internal static void TryRemoveSingleton(
        this IServiceCollection services,
        Type serviceType,
        Type implementationType)
    {
        var descriptor = services.FirstOrDefault(
            x => (x.ServiceType == serviceType) && (x.ImplementationType == implementationType));

        if (descriptor != null)
        {
            _ = services.Remove(descriptor);
        }
    }
}
