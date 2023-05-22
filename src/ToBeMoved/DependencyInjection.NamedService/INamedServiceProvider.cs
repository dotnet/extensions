// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides a mechanism for retrieving named service objects of the specified type.
/// </summary>
/// <typeparam name="TService">The type of the service objects to retrieve.</typeparam>
public interface INamedServiceProvider<out TService>
    where TService : class
{
    /// <summary>
    /// Gets the service object with the specified name.
    /// </summary>
    /// <param name="name">The name of the service object.</param>
    /// <returns>The object.</returns>
    /// <remarks>
    /// This method returns the latest <typeparamref name="TService"/> registered under the name.
    /// </remarks>
    public TService? GetService(string name);

    /// <summary>
    /// Gets all the service objects with the specified name.
    /// </summary>
    /// <param name="name">The name of the service objects.</param>
    /// <returns>The collection of objects.</returns>
    /// <remarks>
    /// This method returns all <typeparamref name="TService"/> registered under the name in the order they were registered.
    /// </remarks>
    public IEnumerable<TService> GetServices(string name);
}
