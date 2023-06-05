// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Validation;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.Probes;

/// <summary>
/// Extension methods for <see cref="TcpEndpointHealthCheckService" /> for <see cref="IServiceCollection" />.
/// </summary>
internal static class TcpEndpointHealthCheckExtensions
{
    /// <summary>
    /// Registers health status reporting using a TCP port
    /// if service is considered as healthy <see cref="IHealthCheck"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add the services to.</param>
    /// <returns>The same instance of the <see cref="IServiceCollection"/> for chaining.</returns>
    internal static IServiceCollection AddTcpEndpointHealthCheck(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        return services.AddTcpEndpointHealthCheck(Microsoft.Extensions.Options.Options.DefaultName);
    }

    /// <summary>
    /// Registers health status reporting using a TCP port
    /// if service is considered as healthy <see cref="IHealthCheck"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add the services to.</param>
    /// <param name="name">Name used to retrieve the options.</param>
    /// <returns>The same instance of the <see cref="IServiceCollection"/> for chaining.</returns>
    internal static IServiceCollection AddTcpEndpointHealthCheck(this IServiceCollection services, string name)
    {
        _ = Throw.IfNull(services);

        _ = services.AddHealthChecks();

        _ = services
            .AddValidatedOptions<TcpEndpointHealthCheckOptions, TcpEndpointHealthCheckOptionsValidator>(name);

        _ = services.AddSingleton<IHostedService>(provider =>
        {
            var options = provider.GetRequiredService<IOptionsMonitor<TcpEndpointHealthCheckOptions>>().Get(name);
            return ActivatorUtilities.CreateInstance<TcpEndpointHealthCheckService>(provider, options);
        });

        return services;
    }

    /// <summary>
    /// Registers health status reporting using a TCP port
    /// if service is considered as healthy <see cref="IHealthCheck"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add the services to.</param>
    /// <param name="configure">Configuration for <see cref="TcpEndpointHealthCheckOptions"/>.</param>
    /// <returns>The same instance of the <see cref="IServiceCollection"/> for chaining.</returns>
    internal static IServiceCollection AddTcpEndpointHealthCheck(
        this IServiceCollection services,
        Action<TcpEndpointHealthCheckOptions> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        _ = services.Configure(configure);

        return services.AddTcpEndpointHealthCheck();
    }

    /// <summary>
    /// Registers health status reporting using a TCP port
    /// if service is considered as healthy <see cref="IHealthCheck"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add the services to.</param>
    /// <param name="name">Name for the options.</param>
    /// <param name="configure">Configuration for <see cref="TcpEndpointHealthCheckOptions"/>.</param>
    /// <returns>The same instance of the <see cref="IServiceCollection"/> for chaining.</returns>
    internal static IServiceCollection AddTcpEndpointHealthCheck(
        this IServiceCollection services,
        string name,
        Action<TcpEndpointHealthCheckOptions> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        _ = services.Configure(name, configure);

        return services.AddTcpEndpointHealthCheck(name);
    }

    /// <summary>
    /// Registers health status reporting using a TCP port
    /// if service is considered as healthy <see cref="IHealthCheck"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add the services to.</param>
    /// <param name="configurationSection">Configuration for <see cref="TcpEndpointHealthCheckOptions"/>.</param>
    /// <returns>The same instance of the <see cref="IServiceCollection"/> for chaining.</returns>
    internal static IServiceCollection AddTcpEndpointHealthCheck(
        this IServiceCollection services,
        IConfigurationSection configurationSection)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configurationSection);

        _ = services.Configure<TcpEndpointHealthCheckOptions>(configurationSection);

        return services.AddTcpEndpointHealthCheck();
    }

    /// <summary>
    /// Registers health status reporting using a TCP port
    /// if service is considered as healthy <see cref="IHealthCheck"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add the services to.</param>
    /// <param name="name">Name for the options.</param>
    /// <param name="configurationSection">Configuration for <see cref="TcpEndpointHealthCheckOptions"/>.</param>
    /// <returns>The same instance of the <see cref="IServiceCollection"/> for chaining.</returns>
    internal static IServiceCollection AddTcpEndpointHealthCheck(
        this IServiceCollection services,
        string name,
        IConfigurationSection configurationSection)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configurationSection);

        _ = services.Configure<TcpEndpointHealthCheckOptions>(name, configurationSection);

        return services.AddTcpEndpointHealthCheck(name);
    }
}
