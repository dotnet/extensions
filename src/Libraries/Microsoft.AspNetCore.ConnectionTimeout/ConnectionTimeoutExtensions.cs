// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Validation;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.AspNetCore.Connections;

/// <summary>
/// Extensions used to register the connection timeout middleware.
/// </summary>
public static class ConnectionTimeoutExtensions
{
    /// <summary>
    /// Add the connection timeout middleware.
    /// </summary>
    /// <param name="listenOptions">The server options to use.</param>
    /// <returns>The value of <paramref name="listenOptions"/>.</returns>
    public static ListenOptions UseConnectionTimeout(this ListenOptions listenOptions)
    {
        _ = Throw.IfNull(listenOptions);

        _ = listenOptions.Use(next =>
        {
            var connectionTimeoutOptions = listenOptions.ApplicationServices.GetRequiredService<IOptions<ConnectionTimeoutOptions>>();
            return new ConnectionTimeoutDelegate(next, connectionTimeoutOptions).OnConnectionAsync;
        });

        return listenOptions;
    }

    /// <summary>
    /// Adds option handling for the connection timeout middleware.
    /// </summary>
    /// <param name="services">The dependency injection container to add the service to.</param>
    /// <param name="configure">Delegate to configure the timeout options.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    public static IServiceCollection AddConnectionTimeout(this IServiceCollection services, Action<ConnectionTimeoutOptions> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        _ = services
            .AddValidatedOptions<ConnectionTimeoutOptions, ConnectionTimeoutValidator>()
            .Configure(configure);

        return services;
    }

    /// <summary>
    /// Adds option handling for the connection timeout middleware.
    /// </summary>
    /// <param name="services">The dependency injection container to add the service to.</param>
    /// <param name="section">The configuration section used to configure the feature.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(ConnectionTimeoutOptions))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed with [DynamicDependency]")]
    public static IServiceCollection AddConnectionTimeout(this IServiceCollection services, IConfigurationSection section)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(section);

        _ = services
            .AddValidatedOptions<ConnectionTimeoutOptions, ConnectionTimeoutValidator>()
            .Bind(section);

        return services;
    }
}
