// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.AI;

/// <summary>Provides an implementation of <see cref="IServiceProvider"/> that contains no services.</summary>
internal sealed class EmptyServiceProvider : IKeyedServiceProvider
{
    /// <summary>Gets a singleton instance of <see cref="EmptyServiceProvider"/>.</summary>
    public static EmptyServiceProvider Instance { get; } = new();

    /// <inheritdoc />
    public object? GetService(Type serviceType) => null;

    /// <inheritdoc />
    public object? GetKeyedService(Type serviceType, object? serviceKey) => null;

    /// <inheritdoc />
    public object GetRequiredKeyedService(Type serviceType, object? serviceKey) =>
        GetKeyedService(serviceType, serviceKey) ??
        throw new InvalidOperationException($"No service for type '{serviceType}' and key '{serviceKey}' has been registered.");
}
