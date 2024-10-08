// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extensions for configuring <see cref="LoggingChatClient"/> instances.</summary>
public static class LoggingChatClientBuilderExtensions
{
    /// <summary>Adds logging to the chat client pipeline.</summary>
    /// <param name="builder">The <see cref="ChatClientBuilder"/>.</param>
    /// <param name="logger">
    /// An optional <see cref="ILogger"/> with which logging should be performed. If not supplied, an instance will be resolved from the service provider.
    /// </param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="LoggingChatClient"/> instance.</param>
    /// <returns>The <paramref name="builder"/>.</returns>
    public static ChatClientBuilder UseLogging(
        this ChatClientBuilder builder, ILogger? logger = null, Action<LoggingChatClient>? configure = null)
    {
        _ = Throw.IfNull(builder);

        return builder.Use((services, innerClient) =>
        {
            logger ??= services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(LoggingChatClient));
            var chatClient = new LoggingChatClient(innerClient, logger);
            configure?.Invoke(chatClient);
            return chatClient;
        });
    }
}
