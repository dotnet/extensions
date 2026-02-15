// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extensions for configuring <see cref="OpenTelemetryRealtimeSession"/> instances.</summary>
[Experimental("MEAI001")]
public static class OpenTelemetryRealtimeSessionBuilderExtensions
{
    /// <summary>
    /// Adds OpenTelemetry support to the realtime session pipeline, following the OpenTelemetry Semantic Conventions for Generative AI systems.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The draft specification this follows is available at <see href="https://opentelemetry.io/docs/specs/semconv/gen-ai/" />.
    /// The specification is still experimental and subject to change; as such, the telemetry output by this session is also subject to change.
    /// </para>
    /// <para>
    /// The following standard OpenTelemetry GenAI conventions are supported:
    /// <list type="bullet">
    ///   <item><c>gen_ai.operation.name</c> - Operation name ("realtime")</item>
    ///   <item><c>gen_ai.request.model</c> - Model name from options</item>
    ///   <item><c>gen_ai.provider.name</c> - Provider name from metadata</item>
    ///   <item><c>gen_ai.response.id</c> - Response ID from ResponseDone messages</item>
    ///   <item><c>gen_ai.usage.input_tokens</c> - Input token count</item>
    ///   <item><c>gen_ai.usage.output_tokens</c> - Output token count</item>
    ///   <item><c>gen_ai.request.max_tokens</c> - Max output tokens from options</item>
    ///   <item><c>gen_ai.system_instructions</c> - Instructions from options (sensitive data)</item>
    ///   <item><c>gen_ai.conversation.id</c> - Conversation ID from response</item>
    ///   <item><c>gen_ai.tool.definitions</c> - Tool definitions (sensitive data)</item>
    ///   <item><c>server.address</c> / <c>server.port</c> - Server endpoint info</item>
    ///   <item><c>error.type</c> - Error type on failures</item>
    /// </list>
    /// </para>
    /// <para>
    /// Additionally, the following realtime-specific custom attributes are supported:
    /// <list type="bullet">
    ///   <item><c>gen_ai.realtime.voice</c> - Voice setting from options</item>
    ///   <item><c>gen_ai.realtime.output_modalities</c> - Output modalities (text, audio)</item>
    ///   <item><c>gen_ai.realtime.voice_speed</c> - Voice speed setting</item>
    ///   <item><c>gen_ai.realtime.session_kind</c> - Session kind (Realtime/Transcription)</item>
    /// </list>
    /// </para>
    /// <para>
    /// Metrics include:
    /// <list type="bullet">
    ///   <item><c>gen_ai.client.operation.duration</c> - Duration histogram</item>
    ///   <item><c>gen_ai.client.token.usage</c> - Token usage histogram</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <param name="builder">The <see cref="RealtimeSessionBuilder"/>.</param>
    /// <param name="loggerFactory">An optional <see cref="ILoggerFactory"/> to use to create a logger for logging events.</param>
    /// <param name="sourceName">An optional source name that will be used on the telemetry data.</param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="OpenTelemetryRealtimeSession"/> instance.</param>
    /// <returns>The <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static RealtimeSessionBuilder UseOpenTelemetry(
        this RealtimeSessionBuilder builder,
        ILoggerFactory? loggerFactory = null,
        string? sourceName = null,
        Action<OpenTelemetryRealtimeSession>? configure = null) =>
        Throw.IfNull(builder).Use((innerSession, services) =>
        {
            loggerFactory ??= services.GetService<ILoggerFactory>();

            var session = new OpenTelemetryRealtimeSession(innerSession, loggerFactory?.CreateLogger(typeof(OpenTelemetryRealtimeSession)), sourceName);
            configure?.Invoke(session);

            return session;
        });
}
