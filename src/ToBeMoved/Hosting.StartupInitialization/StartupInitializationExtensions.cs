// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Testing.Internal;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Validation;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Hosting.Testing;

/// <summary>
/// Extensions for configuring startup initialization.
/// </summary>
public static class StartupInitializationExtensions
{
    /// <summary>
    /// Adds function that will be executed before application starts.
    /// </summary>
    /// <remarks>
    /// Use it for one time initialization logic.
    /// Sequence of execution is not guaranteed.
    /// </remarks>
    /// <param name="services">Service collection use to register initialization function.</param>
    /// <returns>Services passed for further configuration.</returns>
    public static IStartupInitializationBuilder AddStartupInitialization(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        return new StartupInitializationBuilder(services);
    }

    /// <summary>
    /// Adds function that will be executed before application starts.
    /// </summary>
    /// <remarks>
    /// Use it for one time initialization logic.
    /// Sequence of execution is not guaranteed.
    /// </remarks>
    /// <param name="services">Service collection use to register initialization function.</param>
    /// <param name="configure">Configure startup initializers.</param>
    /// <returns>Services passed for further configuration.</returns>
    public static IStartupInitializationBuilder AddStartupInitialization(this IServiceCollection services, Action<StartupInitializationOptions> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        _ = services.AddValidatedOptions<StartupInitializationOptions, StartupInitializationOptionsValidator>()
            .Configure(configure);

        return new StartupInitializationBuilder(services);
    }

    /// <summary>
    /// Adds function that will be executed before application starts.
    /// </summary>
    /// <remarks>
    /// Use it for one time initialization logic.
    /// Sequence of execution is not guaranteed.
    /// </remarks>
    /// <param name="services">Service collection use to register initialization function.</param>
    /// <param name="section">Configure startup initializers with config.</param>
    /// <returns>Services passed for further configuration.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(StartupInitializationOptions))]
    [UnconditionalSuppressMessage("Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed by [DynamicDependency]")]
    public static IStartupInitializationBuilder AddStartupInitialization(this IServiceCollection services, IConfigurationSection section)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(section);

        _ = services.AddValidatedOptions<StartupInitializationOptions, StartupInitializationOptionsValidator>();
        _ = services.Configure<StartupInitializationOptions>(section);

        return new StartupInitializationBuilder(services);
    }
}
