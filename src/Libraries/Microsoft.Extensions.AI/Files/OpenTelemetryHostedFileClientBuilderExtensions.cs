// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extensions for configuring <see cref="OpenTelemetryHostedFileClient"/> instances.</summary>
[Experimental(DiagnosticIds.Experiments.AIFiles, UrlFormat = DiagnosticIds.UrlFormat)]
public static class OpenTelemetryHostedFileClientBuilderExtensions
{
    /// <summary>
    /// Adds OpenTelemetry support to the hosted file client pipeline.
    /// </summary>
    /// <remarks>
    /// Since there is currently no OpenTelemetry Semantic Convention for hosted file operations, this implementation
    /// uses general client span conventions alongside standard <c>file.*</c> registry attributes where applicable.
    /// The telemetry output is subject to change as relevant conventions emerge.
    /// </remarks>
    /// <param name="builder">The <see cref="HostedFileClientBuilder"/>.</param>
    /// <param name="loggerFactory">An optional <see cref="ILoggerFactory"/> to use to create a logger for logging events.</param>
    /// <param name="sourceName">An optional source name that will be used on the telemetry data.</param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="OpenTelemetryHostedFileClient"/> instance.</param>
    /// <returns>The <paramref name="builder"/>.</returns>
    public static HostedFileClientBuilder UseOpenTelemetry(
        this HostedFileClientBuilder builder,
        ILoggerFactory? loggerFactory = null,
        string? sourceName = null,
        Action<OpenTelemetryHostedFileClient>? configure = null) =>
        Throw.IfNull(builder).Use((innerClient, services) =>
        {
            loggerFactory ??= services.GetService<ILoggerFactory>();

            var client = new OpenTelemetryHostedFileClient(innerClient, loggerFactory?.CreateLogger(typeof(OpenTelemetryHostedFileClient)), sourceName);
            configure?.Invoke(client);

            return client;
        });
}
