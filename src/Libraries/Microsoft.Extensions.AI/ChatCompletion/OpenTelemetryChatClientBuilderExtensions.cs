// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extensions for configuring <see cref="OpenTelemetryChatClient"/> instances.</summary>
public static class OpenTelemetryChatClientBuilderExtensions
{
    /// <summary>
    /// Adds OpenTelemetry support to the chat client pipeline, following the OpenTelemetry Semantic Conventions for Generative AI systems.
    /// </summary>
    /// <remarks>
    /// The draft specification this follows is available at <see href="https://opentelemetry.io/docs/specs/semconv/gen-ai/" />.
    /// The specification is still experimental and subject to change; as such, the telemetry output by this client is also subject to change.
    /// </remarks>
    /// <param name="builder">The <see cref="ChatClientBuilder"/>.</param>
    /// <param name="loggerFactory">An optional <see cref="ILoggerFactory"/> to use to create a logger for logging events.</param>
    /// <param name="sourceName">An optional source name that will be used on the telemetry data.</param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="OpenTelemetryChatClient"/> instance.</param>
    /// <returns>The <paramref name="builder"/>.</returns>
    public static ChatClientBuilder UseOpenTelemetry(
        this ChatClientBuilder builder,
        ILoggerFactory? loggerFactory = null,
        string? sourceName = null,
        Action<OpenTelemetryChatClient>? configure = null) =>
        Throw.IfNull(builder).Use((services, innerClient) =>
        {
            loggerFactory ??= services.GetService<ILoggerFactory>();

            var chatClient = new OpenTelemetryChatClient(innerClient, loggerFactory?.CreateLogger(typeof(OpenTelemetryChatClient)), sourceName);
            configure?.Invoke(chatClient);

            return chatClient;
        });
}
