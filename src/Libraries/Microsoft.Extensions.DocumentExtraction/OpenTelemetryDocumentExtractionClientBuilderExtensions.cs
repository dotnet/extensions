// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DocumentExtraction;

/// <summary>Provides extensions for configuring <see cref="OpenTelemetryDocumentExtractionClient"/> instances.</summary>
[Experimental(DiagnosticIds.Experiments.DocumentExtraction, UrlFormat = DiagnosticIds.UrlFormat)]
public static class OpenTelemetryDocumentExtractionClientBuilderExtensions
{
    /// <summary>
    /// Adds OpenTelemetry support to the OCR client pipeline, following the OpenTelemetry Semantic Conventions for Generative AI systems.
    /// </summary>
    /// <remarks>
    /// The draft specification this follows is available at <see href="https://opentelemetry.io/docs/specs/semconv/gen-ai/" />.
    /// The specification is still experimental and subject to change; as such, the telemetry output by this client is also subject to change.
    /// </remarks>
    /// <param name="builder">The <see cref="DocumentExtractionClientBuilder"/>.</param>
    /// <param name="loggerFactory">An optional <see cref="ILoggerFactory"/> to use to create a logger for logging events.</param>
    /// <param name="sourceName">An optional source name that will be used on the telemetry data.</param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="OpenTelemetryDocumentExtractionClient"/> instance.</param>
    /// <returns>The <paramref name="builder"/>.</returns>
    public static DocumentExtractionClientBuilder UseOpenTelemetry(
        this DocumentExtractionClientBuilder builder,
        ILoggerFactory? loggerFactory = null,
        string? sourceName = null,
        Action<OpenTelemetryDocumentExtractionClient>? configure = null) =>
        Throw.IfNull(builder).Use((innerClient, services) =>
        {
            loggerFactory ??= services.GetService<ILoggerFactory>();

            var client = new OpenTelemetryDocumentExtractionClient(innerClient, loggerFactory?.CreateLogger(typeof(OpenTelemetryDocumentExtractionClient)), sourceName);
            configure?.Invoke(client);

            return client;
        });
}
