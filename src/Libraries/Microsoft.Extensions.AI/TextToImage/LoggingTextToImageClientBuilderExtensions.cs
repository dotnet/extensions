// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extensions for configuring <see cref="LoggingImageClient"/> instances.</summary>
[Experimental("MEAI001")]
public static class LoggingImageClientBuilderExtensions
{
    /// <summary>Adds logging to the text to image client pipeline.</summary>
    /// <param name="builder">The <see cref="ImageClientBuilder"/>.</param>
    /// <param name="loggerFactory">
    /// An optional <see cref="ILoggerFactory"/> used to create a logger with which logging should be performed.
    /// If not supplied, a required instance will be resolved from the service provider.
    /// </param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="LoggingImageClient"/> instance.</param>
    /// <returns>The <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// When the employed <see cref="ILogger"/> enables <see cref="Logging.LogLevel.Trace"/>, the contents of
    /// prompts and options are logged. These prompts and options may contain sensitive application data.
    /// <see cref="Logging.LogLevel.Trace"/> is disabled by default and should never be enabled in a production environment.
    /// Prompts and options are not logged at other logging levels.
    /// </para>
    /// </remarks>
    public static ImageClientBuilder UseLogging(
        this ImageClientBuilder builder,
        ILoggerFactory? loggerFactory = null,
        Action<LoggingImageClient>? configure = null)
    {
        _ = Throw.IfNull(builder);

        return builder.Use((innerClient, services) =>
        {
            loggerFactory ??= services.GetRequiredService<ILoggerFactory>();

            // If the factory we resolve is for the null logger, the LoggingImageClient will end up
            // being an expensive nop, so skip adding it and just return the inner client.
            if (loggerFactory == NullLoggerFactory.Instance)
            {
                return innerClient;
            }

            var imageClient = new LoggingImageClient(innerClient, loggerFactory.CreateLogger(typeof(LoggingImageClient)));
            configure?.Invoke(imageClient);
            return imageClient;
        });
    }
}
