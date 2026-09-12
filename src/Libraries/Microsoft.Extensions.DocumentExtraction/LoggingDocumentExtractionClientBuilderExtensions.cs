// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DocumentExtraction;

/// <summary>Provides extensions for configuring <see cref="LoggingDocumentExtractionClient"/> instances.</summary>
[Experimental(DiagnosticIds.Experiments.DocumentExtraction, UrlFormat = DiagnosticIds.UrlFormat)]
public static class LoggingDocumentExtractionClientBuilderExtensions
{
    /// <summary>Adds logging to the OCR client pipeline.</summary>
    /// <param name="builder">The <see cref="DocumentExtractionClientBuilder"/>.</param>
    /// <param name="loggerFactory">
    /// An optional <see cref="ILoggerFactory"/> used to create a logger with which logging should be performed.
    /// If not supplied, a required instance will be resolved from the service provider.
    /// </param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="LoggingDocumentExtractionClient"/> instance.</param>
    /// <returns>The <paramref name="builder"/>.</returns>
    /// <remarks>
    /// <para>
    /// When the employed <see cref="ILogger"/> enables <see cref="Logging.LogLevel.Trace"/>, the contents of
    /// options and results are logged. These may contain sensitive application data.
    /// <see cref="Logging.LogLevel.Trace"/> is disabled by default and should never be enabled in a production environment.
    /// Options and results are not logged at other logging levels.
    /// </para>
    /// </remarks>
    public static DocumentExtractionClientBuilder UseLogging(
        this DocumentExtractionClientBuilder builder,
        ILoggerFactory? loggerFactory = null,
        Action<LoggingDocumentExtractionClient>? configure = null)
    {
        _ = Throw.IfNull(builder);

        return builder.Use((innerClient, services) =>
        {
            loggerFactory ??= services.GetRequiredService<ILoggerFactory>();

            // If the factory we resolve is for the null logger, the LoggingDocumentExtractionClient will end up
            // being an expensive nop, so skip adding it and just return the inner client.
            if (loggerFactory == NullLoggerFactory.Instance)
            {
                return innerClient;
            }

            var documentExtractionClient = new LoggingDocumentExtractionClient(innerClient, loggerFactory.CreateLogger(typeof(LoggingDocumentExtractionClient)));
            configure?.Invoke(documentExtractionClient);
            return documentExtractionClient;
        });
    }
}
