// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options.Validation;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Hosting.Testing.Internal;

/// <summary>
/// Builds server initialization phase.
/// </summary>
internal sealed class StartupInitializationBuilder : IStartupInitializationBuilder
{
    /// <summary>
    /// Gets services used to configure initializers.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupInitializationBuilder"/> class.
    /// </summary>
    /// <param name="services">Service collection used for configuration.</param>
    public StartupInitializationBuilder(IServiceCollection services)
    {
        Services = services;

        RegisterHostedService(services);
        _ = Services.AddValidatedOptions<StartupInitializationOptions, StartupInitializationOptionsValidator>();
    }

    /// <summary>
    /// Adds initializer of given type to be executed at service startup.
    /// </summary>
    /// <typeparam name="T">Type of the initializer to add.</typeparam>
    /// <returns>Instance of <see cref="StartupInitializationBuilder"/> for further configuration.</returns>
    public IStartupInitializationBuilder AddInitializer<T>()
        where T : class, IStartupInitializer
    {
        Services.TryAddTransient<IStartupInitializer, T>();

        return this;
    }

    /// <inheritdoc/>
    public IStartupInitializationBuilder AddInitializer(Func<IServiceProvider, CancellationToken, Task> initializer)
    {
        _ = Throw.IfNull(initializer);

        _ = Services.AddTransient<IStartupInitializer>(provider => new FunctionDerivedInitializer(provider, initializer));

        return this;
    }

    private static void RegisterHostedService(IServiceCollection services)
    {
        if (services.Count != 0 && services[0].ImplementationType == typeof(StartupHostedService))
        {
            return;
        }

        services
            .RemoveAll<StartupHostedService>()
            .Insert(0, ServiceDescriptor.Singleton<IHostedService, StartupHostedService>());
    }
}
