// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.Http.Resilience.Internal;

/// <summary>
/// Resilience based extensions for <see cref="IServiceCollection"/>.
/// </summary>
internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds a implementation of <see cref="IRequestClonerInternal"/> to services.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <returns>The same services instances.</returns>
    public static IServiceCollection AddRequestCloner(this IServiceCollection services)
    {
        services.TryAddSingleton<IRequestClonerInternal, DefaultRequestCloner>();
        return services;
    }
}
