// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for getting services from an <see cref="INamedServiceProvider{T}"/>.
/// </summary>
public static class NamedServiceProviderExtensions
{
    /// <summary>
    /// Get service of type <typeparamref name="TService"/> from the <see cref="INamedServiceProvider{T}"/>.
    /// </summary>
    /// <typeparam name="TService">The type of service object to get.</typeparam>
    /// <param name="provider">The <see cref="INamedServiceProvider{T}"/> to retrieve the service object from.</param>
    /// <param name="name">The name of the service.</param>
    /// <returns>A service object of type <typeparamref name="TService"/>.</returns>
    /// <exception cref="System.InvalidOperationException">There is no service of type <typeparamref name="TService"/> with the name.</exception>
    /// <remarks>
    /// This method returns the latest <typeparamref name="TService"/> registered under the name.
    /// </remarks>
    public static TService GetRequiredService<TService>(this INamedServiceProvider<TService> provider, string name)
        where TService : class
    {
        _ = Throw.IfNull(provider);
        _ = Throw.IfNullOrEmpty(name);

        var service = provider.GetService(name);
        if (service == null)
        {
            Throw.InvalidOperationException($"No service for type '${typeof(TService)}' and name '${name}' has been registered.");
        }

        return service;
    }
}
