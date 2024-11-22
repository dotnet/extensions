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
    /// If not supplied, an instance will be resolved from the service provider.
    /// If no instance is available, no logging will be performed.
    /// </param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="LoggingChatClient"/> instance.</param>
    /// <returns>The <paramref name="builder"/>.</returns>
    /// <remarks>
    /// The resulting logging will be to an <see cref="ILogger"/> created by the <paramref name="loggerFactory"/>,
    /// or if no <paramref name="loggerFactory"/> is supplied, by a <see cref="ILoggerFactory"/> queried from
    /// the services in <paramref name="builder"/>. If no <see cref="ILoggerFactory"/> is available, no logging
    /// will be performed.
    /// </remarks>
    public static ChatClientBuilder UseLogging(
        this ChatClientBuilder builder,
        ILoggerFactory? loggerFactory = null,
        Action<LoggingChatClient>? configure = null)
    {
        _ = Throw.IfNull(builder);

        return builder.Use((innerClient, services) =>
        {
            // If no factory was specified, try to resolve one from the service provider.
            // Then if we still couldn't get one, or if we got the null logger factory,
            // there's no point in creating a logging client, as it'll be a nop, so just
            // skip it. As an alternative design, this could throw an exception, but that
            // then leads consumers to do this check on their own, querying the service provider
            // to see if it includes a logger factory and only calling UseLogging if it does,
            // which both negates the fluent API and duplicates the check done here.
            loggerFactory ??= services.GetService<ILoggerFactory>();
            if (loggerFactory is null || loggerFactory == NullLoggerFactory.Instance)
            {
                return innerClient;
            }

            var chatClient = new LoggingChatClient(innerClient, loggerFactory.CreateLogger(typeof(LoggingChatClient)));
            configure?.Invoke(chatClient);
            return chatClient;
        });
    }
}
