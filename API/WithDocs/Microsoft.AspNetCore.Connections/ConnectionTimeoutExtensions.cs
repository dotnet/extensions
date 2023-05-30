// Assembly 'Microsoft.AspNetCore.ConnectionTimeout'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
    /// <returns>The value of <paramref name="listenOptions" />.</returns>
    public static ListenOptions UseConnectionTimeout(this ListenOptions listenOptions);

    /// <summary>
    /// Adds option handling for the connection timeout middleware.
    /// </summary>
    /// <param name="services">The dependency injection container to add the service to.</param>
    /// <param name="configure">Delegate to configure the timeout options.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddConnectionTimeout(this IServiceCollection services, Action<ConnectionTimeoutOptions> configure);

    /// <summary>
    /// Adds option handling for the connection timeout middleware.
    /// </summary>
    /// <param name="services">The dependency injection container to add the service to.</param>
    /// <param name="section">The configuration section used to configure the feature.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddConnectionTimeout(this IServiceCollection services, IConfigurationSection section);
}
