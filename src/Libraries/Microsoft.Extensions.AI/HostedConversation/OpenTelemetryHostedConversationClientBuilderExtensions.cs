// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extensions for configuring <see cref="OpenTelemetryHostedConversationClient"/> instances.</summary>
[Experimental(DiagnosticIds.Experiments.AIHostedConversation, UrlFormat = DiagnosticIds.UrlFormat)]
public static class OpenTelemetryHostedConversationClientBuilderExtensions
{
    /// <summary>
    /// Adds OpenTelemetry support to the hosted conversation client pipeline.
    /// </summary>
    /// <remarks>
    /// Since there is currently no OpenTelemetry Semantic Convention for hosted conversation operations, this implementation
    /// uses general client span conventions alongside standard <c>conversations.*</c> registry attributes where applicable.
    /// The telemetry output is subject to change as relevant conventions emerge.
    /// </remarks>
    /// <param name="builder">The <see cref="HostedConversationClientBuilder"/>.</param>
    /// <param name="loggerFactory">An optional <see cref="ILoggerFactory"/> to use to create a logger for logging events.</param>
    /// <param name="sourceName">An optional source name that will be used on the telemetry data.</param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="OpenTelemetryHostedConversationClient"/> instance.</param>
    /// <returns>The <paramref name="builder"/>.</returns>
    public static HostedConversationClientBuilder UseOpenTelemetry(
        this HostedConversationClientBuilder builder,
        ILoggerFactory? loggerFactory = null,
        string? sourceName = null,
        Action<OpenTelemetryHostedConversationClient>? configure = null) =>
        Throw.IfNull(builder).Use((innerClient, services) =>
        {
            loggerFactory ??= services.GetService<ILoggerFactory>();

            var client = new OpenTelemetryHostedConversationClient(innerClient, loggerFactory?.CreateLogger(typeof(OpenTelemetryHostedConversationClient)), sourceName);
            configure?.Invoke(client);

            return client;
        });
}
