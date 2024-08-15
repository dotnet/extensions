// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Throw = Microsoft.Shared.Diagnostics.Throw;

namespace Microsoft.Shared.Diagnostics;

/// <summary>
/// Adds debugger to DI container.
/// </summary>
#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif

internal static class DebuggerExtensions
{
    /// <summary>
    /// Registers system debugger as <see cref="IDebuggerState"/> interface.
    /// </summary>
    /// <param name="services">Service collection to register system debugger in.</param>
    /// <returns>Passed instance of service collection for further configuration.</returns>
    public static IServiceCollection AddSystemDebuggerState(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        services.TryAddSingleton<IDebuggerState>(DebuggerState.System);

        return services;
    }

    /// <summary>
    /// Registers system debugger as <see cref="IDebuggerState"/> interface.
    /// </summary>
    /// <param name="services">Service collection to register system debugger in.</param>
    /// <returns>Passed instance of service collection for further configuration.</returns>
    public static IServiceCollection AddAttachedDebuggerState(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        services.TryAddSingleton<IDebuggerState>(DebuggerState.Attached);

        return services;
    }

    /// <summary>
    /// Registers system debugger as <see cref="IDebuggerState"/> interface.
    /// </summary>
    /// <param name="services">Service collection to register system debugger in.</param>
    /// <returns>Passed instance of service collection for further configuration.</returns>
    public static IServiceCollection AddDetachedDebuggerState(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        services.TryAddSingleton<IDebuggerState>(DebuggerState.Detached);

        return services;
    }
}
