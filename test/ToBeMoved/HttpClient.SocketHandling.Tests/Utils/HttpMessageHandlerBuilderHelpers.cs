// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.HttpClient.SocketHandling.Test.Utils;

public static class HttpMessageHandlerBuilderHelpers
{
    public static HttpMessageHandler ResolveHttpPrimaryHandler<T>(this IServiceProvider services)
    {
        var name = typeof(T).Name;
        var optionsMonitor = services.GetRequiredService<IOptionsMonitor<HttpClientFactoryOptions>>();
        var options = optionsMonitor.Get(name);

        var builder = services.GetRequiredService<HttpMessageHandlerBuilder>();
        builder.Name = name;

        foreach (var action in options.HttpMessageHandlerBuilderActions)
        {
            action(builder);
        }

        return builder.PrimaryHandler;
    }

    public static SocketsHttpHandlerOptions ExtractOptions(this HttpMessageHandler messageHandler)
    {
        var handler = (SocketsHttpHandler)messageHandler;

        return new SocketsHttpHandlerOptions
        {
            AllowAutoRedirect = handler.AllowAutoRedirect,
            UseCookies = handler.UseCookies,
            MaxConnectionsPerServer = handler.MaxConnectionsPerServer,
            AutomaticDecompression = handler.AutomaticDecompression,
            ConnectTimeout = handler.ConnectTimeout,
            PooledConnectionLifetime = handler.PooledConnectionLifetime,
            PooledConnectionIdleTimeout = handler.PooledConnectionIdleTimeout,
#if NET8_0_OR_GREATER
            // Whilst these API are marked as NET5_0_OR_GREATER we don't build .NET 5.0,
            // and as such the API is available in .NET 8 onwards.
            KeepAlivePingDelay = handler.KeepAlivePingDelay,
            KeepAlivePingTimeout = handler.KeepAlivePingTimeout,
#endif
        };
    }
}
