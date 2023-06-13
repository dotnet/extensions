// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Telemetry.Latency;

/// <summary>
/// Extensions to add a no-op latency context.
/// </summary>
public static class NullLatencyContextExtensions
{
    /// <summary>
    /// Add a no-op latency context to a dependency injection container.
    /// </summary>
    /// <param name="services">The dependency injection container to add the context to.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddNullLatencyContext(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        services.TryAddSingleton<ILatencyContextProvider, NullLatencyContext>();
        services.TryAddSingleton<ILatencyContextTokenIssuer, NullLatencyContext>();

        return services;
    }
}
