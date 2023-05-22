// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting.Testing;

/// <summary>
/// Configure service startup initialization.
/// </summary>
public interface IStartupInitializationBuilder
{
    /// <summary>
    /// Gets services used add initializers.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Adds initializer of given type to be executed at service startup.
    /// </summary>
    /// <remarks>
    /// The initializers should be pure functions, i.e. they shouldn't hold any state.
    /// They are used in transient manner, and the implementation is not guaranteed to be reachable by GC after startup time.
    /// </remarks>
    /// <typeparam name="T">Type of the initializer to add.</typeparam>
    /// <returns>Instance of <see cref="IStartupInitializationBuilder"/> for further configuration.</returns>
    public IStartupInitializationBuilder AddInitializer<T>()
        where T : class, IStartupInitializer;

    /// <summary>
    /// Add ad-hoc initializer to be executed at service startup.
    /// </summary>
    /// <remarks>
    /// Note, that there is no indempotency semantics while calling this API.
    /// Therefore, this interface is not recommended for library authors.
    /// </remarks>
    /// <param name="initializer">Initializer to execute.</param>
    /// <returns>Instance of <see cref="IStartupInitializationBuilder"/> for further configuration.</returns>
    public IStartupInitializationBuilder AddInitializer(Func<IServiceProvider, CancellationToken, Task> initializer);
}
