// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.AsyncState;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions to manipulate async state.
/// </summary>
public static class AsyncStateExtensions
{
    /// <summary>
    /// Adds default implementations for <see cref="IAsyncState"/>, <see cref="IAsyncContext{T}"/>, and <see cref="IAsyncLocalContext{T}"/> services.
    /// </summary>
    /// <param name="services">The dependency injection container to add the implementations to.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null" />.</exception>
    public static IServiceCollection AddAsyncState(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        services.TryAddSingleton(typeof(IAsyncContext<>), typeof(AsyncContext<>));
        services.TryAddActivatedSingleton<IAsyncState, Microsoft.Extensions.AsyncState.AsyncState>();
#pragma warning disable EXTEXP0006 // Type is for evaluation purposes only and is subject to change or removal in future updates
        services.TryAddSingleton(typeof(IAsyncLocalContext<>), typeof(AsyncContext<>));
#pragma warning restore EXTEXP0006 // Type is for evaluation purposes only and is subject to change or removal in future updates

        return services;
    }
}
