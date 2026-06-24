// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extensions for configuring <see cref="LoggingHostedFileClient"/> instances.</summary>
[Experimental(DiagnosticIds.Experiments.AIFiles, UrlFormat = DiagnosticIds.UrlFormat)]
public static class LoggingHostedFileClientBuilderExtensions
{
    /// <summary>Adds logging to the hosted file client pipeline.</summary>
    /// <param name="builder">The <see cref="HostedFileClientBuilder"/>.</param>
    /// <param name="loggerFactory">
    /// An optional <see cref="ILoggerFactory"/> used to create a logger with which logging should be performed.
    /// If not supplied, a required instance will be resolved from the service provider.
    /// </param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="LoggingHostedFileClient"/> instance.</param>
    /// <returns>The <paramref name="builder"/>.</returns>
    /// <remarks>
    /// <para>
    /// When the employed <see cref="ILogger"/> enables <see cref="Logging.LogLevel.Trace"/>, the contents of
    /// options and results are logged. These may contain sensitive application data.
    /// <see cref="Logging.LogLevel.Trace"/> is disabled by default and should never be enabled in a production environment.
    /// Options and results are not logged at other logging levels.
    /// </para>
    /// </remarks>
    public static HostedFileClientBuilder UseLogging(
        this HostedFileClientBuilder builder,
        ILoggerFactory? loggerFactory = null,
        Action<LoggingHostedFileClient>? configure = null)
    {
        _ = Throw.IfNull(builder);

        return builder.Use((innerClient, services) =>
        {
            loggerFactory ??= services.GetRequiredService<ILoggerFactory>();

            // If the factory we resolve is for the null logger, the LoggingHostedFileClient will end up
            // being an expensive nop, so skip adding it and just return the inner client.
            if (loggerFactory == NullLoggerFactory.Instance)
            {
                return innerClient;
            }

            var fileClient = new LoggingHostedFileClient(innerClient, loggerFactory.CreateLogger(typeof(LoggingHostedFileClient)));
            configure?.Invoke(fileClient);
            return fileClient;
        });
    }
}
