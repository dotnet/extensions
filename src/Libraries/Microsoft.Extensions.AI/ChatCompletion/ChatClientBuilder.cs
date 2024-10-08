// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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
}
