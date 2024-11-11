// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>A builder for creating pipelines of <see cref="IChatClient"/>.</summary>
public sealed class ChatClientBuilder
{
    /// <summary>The registered client factory instances.</summary>
    private List<Func<IServiceProvider, IChatClient, IChatClient>>? _clientFactories;

    /// <summary>Initializes a new instance of the <see cref="ChatClientBuilder"/> class.</summary>
    /// <param name="services">The service provider to use for dependency injection.</param>
    public ChatClientBuilder(IServiceProvider? services = null)
    {
        Services = services ?? EmptyServiceProvider.Instance;
    }

    /// <summary>Gets the <see cref="IServiceProvider"/> associated with the builder instance.</summary>
    public IServiceProvider Services { get; }

    /// <summary>Completes the pipeline by adding a final <see cref="IChatClient"/> that represents the underlying backend. This is typically a client for an LLM service.</summary>
    /// <param name="innerClient">The inner client to use.</param>
    /// <returns>An instance of <see cref="IChatClient"/> that represents the entire pipeline. Calls to this instance will pass through each of the pipeline stages in turn.</returns>
    public IChatClient Use(IChatClient innerClient)
    {
        var chatClient = Throw.IfNull(innerClient);

        // To match intuitive expectations, apply the factories in reverse order, so that the first factory added is the outermost.
        if (_clientFactories is not null)
        {
            for (var i = _clientFactories.Count - 1; i >= 0; i--)
            {
                chatClient = _clientFactories[i](Services, chatClient) ??
                    throw new InvalidOperationException(
                        $"The {nameof(ChatClientBuilder)} entry at index {i} returned null. " +
                        $"Ensure that the callbacks passed to {nameof(Use)} return non-null {nameof(IChatClient)} instances.");
            }
        }

        return chatClient;
    }

    /// <summary>Adds a factory for an intermediate chat client to the chat client pipeline.</summary>
    /// <param name="clientFactory">The client factory function.</param>
    /// <returns>The updated <see cref="ChatClientBuilder"/> instance.</returns>
    public ChatClientBuilder Use(Func<IChatClient, IChatClient> clientFactory)
    {
        _ = Throw.IfNull(clientFactory);

        return Use((_, innerClient) => clientFactory(innerClient));
    }

    /// <summary>Adds a factory for an intermediate chat client to the chat client pipeline.</summary>
    /// <param name="clientFactory">The client factory function.</param>
    /// <returns>The updated <see cref="ChatClientBuilder"/> instance.</returns>
    public ChatClientBuilder Use(Func<IServiceProvider, IChatClient, IChatClient> clientFactory)
    {
        _ = Throw.IfNull(clientFactory);

        (_clientFactories ??= []).Add(clientFactory);
        return this;
    }

    /// <summary>
    /// Adds a callback that configures a <see cref="ChatOptions"/> to be passed to the next client in the pipeline.
    /// </summary>
    /// <param name="configure">
    /// The delegate to invoke to configure the <see cref="ChatOptions"/> instance.
    /// It is passed a clone of the caller-supplied <see cref="ChatOptions"/> instance (or a newly constructed instance if the caller-supplied instance is <see langword="null"/>).
    /// </param>
    /// <remarks>
    /// This can be used to set default options. The <paramref name="configure"/> delegate is passed either a new instance of
    /// <see cref="ChatOptions"/> if the caller didn't supply a <see cref="ChatOptions"/> instance, or a clone (via <see cref="ChatOptions.Clone"/>
    /// of the caller-supplied instance if one was supplied.
    /// </remarks>
    /// <returns>The current builder instance.</returns>
    public ChatClientBuilder ConfigureOptions(Action<ChatOptions> configure)
    {
        _ = Throw.IfNull(configure);

        return Use(innerClient => new ConfigureOptionsChatClient(innerClient, configure));
    }

    /// <summary>
    /// Adds a <see cref="DistributedCachingChatClient"/> as the next stage in the pipeline.
    /// </summary>
    /// <param name="storage">
    /// An optional <see cref="IDistributedCache"/> instance that will be used as the backing store for the cache. If not supplied, an instance will be resolved from the service provider.
    /// </param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="DistributedCachingChatClient"/> instance.</param>
    /// <returns>The current builder instance.</returns>
    public ChatClientBuilder UseDistributedCache(IDistributedCache? storage = null, Action<DistributedCachingChatClient>? configure = null)
    {
        return Use((services, innerClient) =>
        {
            storage ??= services.GetRequiredService<IDistributedCache>();
            var chatClient = new DistributedCachingChatClient(innerClient, storage);
            configure?.Invoke(chatClient);
            return chatClient;
        });
    }

    /// <summary>
    /// Enables automatic function call invocation on the chat pipeline.
    /// </summary>
    /// <remarks>This works by adding an instance of <see cref="FunctionInvokingChatClient"/> with default options.</remarks>
    /// <param name="loggerFactory">An optional <see cref="ILoggerFactory"/> to use to create a logger for logging function invocations.</param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="FunctionInvokingChatClient"/> instance.</param>
    /// <returns>The current builder instance.</returns>
    public ChatClientBuilder UseFunctionInvocation(
        ILoggerFactory? loggerFactory = null,
        Action<FunctionInvokingChatClient>? configure = null)
    {
        return Use((services, innerClient) =>
        {
            loggerFactory ??= services.GetService<ILoggerFactory>();

            var chatClient = new FunctionInvokingChatClient(innerClient, loggerFactory?.CreateLogger(typeof(FunctionInvokingChatClient)));
            configure?.Invoke(chatClient);
            return chatClient;
        });
    }

    /// <summary>Adds logging to the chat client pipeline.</summary>
    /// <param name="logger">
    /// An optional <see cref="ILogger"/> with which logging should be performed. If not supplied, an instance will be resolved from the service provider.
    /// </param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="LoggingChatClient"/> instance.</param>
    /// <returns>The current builder instance.</returns>
    public ChatClientBuilder UseLogging(
        ILogger? logger = null, Action<LoggingChatClient>? configure = null)
    {
        return Use((services, innerClient) =>
        {
            logger ??= services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(LoggingChatClient));
            var chatClient = new LoggingChatClient(innerClient, logger);
            configure?.Invoke(chatClient);
            return chatClient;
        });
    }

    /// <summary>
    /// Adds OpenTelemetry support to the chat client pipeline, following the OpenTelemetry Semantic Conventions for Generative AI systems.
    /// </summary>
    /// <remarks>
    /// The draft specification this follows is available at <see href="https://opentelemetry.io/docs/specs/semconv/gen-ai/" />.
    /// The specification is still experimental and subject to change; as such, the telemetry output by this client is also subject to change.
    /// </remarks>
    /// <param name="loggerFactory">An optional <see cref="ILoggerFactory"/> to use to create a logger for logging events.</param>
    /// <param name="sourceName">An optional source name that will be used on the telemetry data.</param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="OpenTelemetryChatClient"/> instance.</param>
    /// <returns>The current builder instance.</returns>
    public ChatClientBuilder UseOpenTelemetry(
        ILoggerFactory? loggerFactory = null,
        string? sourceName = null,
        Action<OpenTelemetryChatClient>? configure = null)
    {
        return Use((services, innerClient) =>
        {
            loggerFactory ??= services.GetService<ILoggerFactory>();

            var chatClient = new OpenTelemetryChatClient(innerClient, loggerFactory?.CreateLogger(typeof(OpenTelemetryChatClient)), sourceName);
            configure?.Invoke(chatClient);

            return chatClient;
        });
    }
}
