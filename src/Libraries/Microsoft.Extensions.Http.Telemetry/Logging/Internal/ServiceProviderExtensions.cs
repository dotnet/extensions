// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Internal;

internal static class ServiceProviderExtensions
{
    /// <summary>
    /// Gets a keyed service from the <see cref="IServiceProvider"/>, or a non-keyed service if the key is <see langword="null"/>.
    /// </summary>
    /// <typeparam name="T">The type of service object to get.</typeparam>
    /// <param name="provider">The <see cref="IServiceProvider"/> to retrieve the service object from.</param>
    /// <param name="serviceKey">An optional object that specifies the key of service object to get.</param>
    /// <returns>A service object of type <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidOperationException">There is no service of type <typeparamref name="T"/> registered.</exception>
    public static T GetRequiredOrKeyedRequiredService<T>(this IServiceProvider provider, string? serviceKey)
        where T : notnull
    {
        return serviceKey is null
            ? provider.GetRequiredService<T>()
            : provider.GetRequiredKeyedService<T>(serviceKey);
    }
}
