// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.HttpClient.SocketHandling;

/// <summary>
/// Extension methods for configuring an <see cref="IHttpClientBuilder"/>.
/// </summary>
public static class HttpClientSocketHandlingExtensions
{
    /// <summary>
    /// Adds a delegate that will set <see cref="SocketsHttpHandler"/> as the primary <see cref="HttpMessageHandler"/>
    /// for a named <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
    /// <returns>The given <see cref="IHttpClientBuilder"/> instance to allow method chaining.</returns>
    public static IHttpClientBuilder AddSocketsHttpHandler(this IHttpClientBuilder builder)
    {
        _ = Throw.IfNull(builder);

        var builderName = builder.Name;
        _ = builder.ConfigurePrimaryHttpMessageHandler(provider =>
        {
            var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<SocketsHttpHandlerOptions>>();
            var options = optionsMonitor.Get(builderName);
            return new SocketsHttpHandler
            {
                AutomaticDecompression = options.AutomaticDecompression,
                AllowAutoRedirect = options.AllowAutoRedirect,
                ConnectTimeout = options.ConnectTimeout,
                MaxConnectionsPerServer = options.MaxConnectionsPerServer,
                PooledConnectionLifetime = options.PooledConnectionLifetime,
                PooledConnectionIdleTimeout = options.PooledConnectionIdleTimeout,
#if NET5_0_OR_GREATER
                KeepAlivePingDelay = options.KeepAlivePingDelay,
                KeepAlivePingTimeout = options.KeepAlivePingTimeout,
#endif
                UseCookies = options.UseCookies
            };
        });

        return builder;
    }

    /// <summary>
    /// Adds a delegate that will set <see cref="SocketsHttpHandler"/> as the primary <see cref="HttpMessageHandler"/>
    /// for a named <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
    /// <param name="configure">Configure using a <see cref="SocketsHttpHandlerBuilder"/> instance.</param>
    /// <returns>The given <see cref="IHttpClientBuilder"/> instance to allow method chaining.</returns>
    public static IHttpClientBuilder AddSocketsHttpHandler(this IHttpClientBuilder builder, Action<SocketsHttpHandlerBuilder> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        _ = builder.AddSocketsHttpHandler();
        configure(new SocketsHttpHandlerBuilder(builder));

        return builder;
    }
}
