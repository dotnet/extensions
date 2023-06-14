// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.AsyncState;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.AspNetCore.AsyncState;

/// <summary>
/// Extension methods to add the async state feature with the HttpContext lifetime to a dependency injection container.
/// </summary>
public static class AsyncStateHttpContextExtensions
{
    /// <summary>
    /// Adds default implementations for <see cref="IAsyncState"/>, <see cref="IAsyncContext{T}"/>, and <see cref="IAsyncLocalContext{T}"/> services,
    /// scoped to the lifetime of <see cref="Microsoft.AspNetCore.Http.HttpContext"/> instances.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null" />.</exception>
    public static IServiceCollection AddAsyncStateHttpContext(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        services
            .AddHttpContextAccessor()
            .AddAsyncStateCore()
            .TryRemoveAsyncStateCore()
            .TryAddSingleton(typeof(IAsyncContext<>), typeof(AsyncContextHttpContext<>));

        return services;
    }
}
