// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.Telemetry.Metering;

/// <summary>
/// Extensions to control metering integration.
/// </summary>
public static class MeteringExtensions
{
    /// <summary>
    /// Registers <see cref="Meter{T}"/> to a dependency injection container.
    /// </summary>
    /// <param name="services">The dependency injection container to register metering into.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    [Experimental]
    public static IServiceCollection RegisterMetering(this IServiceCollection services)
    {
        services.TryAdd(ServiceDescriptor.Singleton(typeof(Meter<>), typeof(Meter<>)));
        return services;
    }
}
