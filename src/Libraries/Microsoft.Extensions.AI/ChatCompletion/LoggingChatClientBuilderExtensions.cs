// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extensions for configuring <see cref="LoggingChatClient"/> instances.</summary>
public static class LoggingChatClientBuilderExtensions
{
    /// <summary>Adds logging to the chat client pipeline.</summary>
    /// <param name="builder">The <see cref="ChatClientBuilder"/>.</param>
    /// <param name="loggerFactory">
    /// An optional <see cref="ILoggerFactory"/> used to create a logger with which logging should be performed.
    /// If not supplied, a required instance will be resolved from the service provider.
    /// </param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="LoggingChatClient"/> instance.</param>
    /// <returns>The <paramref name="builder"/>.</returns>
    public static ChatClientBuilder UseLogging(
        this ChatClientBuilder builder,
        ILoggerFactory? loggerFactory = null,
        Action<LoggingChatClient>? configure = null)
    {
        _ = Throw.IfNull(builder);

        return builder.Use((innerClient, services) =>
        {
            loggerFactory ??= services.GetRequiredService<ILoggerFactory>();

            // If the factory we resolve is for the null logger, the LoggingChatClient will end up
            // being an expensive nop, so skip adding it and just return the inner client.
            if (loggerFactory == NullLoggerFactory.Instance)
            {
                return innerClient;
            }

            var chatClient = new LoggingChatClient(innerClient, loggerFactory.CreateLogger(typeof(LoggingChatClient)));
            configure?.Invoke(chatClient);
            return chatClient;
        });
    }
}
