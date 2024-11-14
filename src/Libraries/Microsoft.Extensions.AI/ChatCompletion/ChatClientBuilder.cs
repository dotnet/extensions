// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>A builder for creating pipelines of <see cref="IChatClient"/>.</summary>
public sealed class ChatClientBuilder
{
    private Func<IServiceProvider, IChatClient> _innerClientFactory;

    /// <summary>The registered client factory instances.</summary>
    private List<Func<IServiceProvider, IChatClient, IChatClient>>? _clientFactories;

    /// <summary>Initializes a new instance of the <see cref="ChatClientBuilder"/> class.</summary>
    /// <param name="innerClient">The inner <see cref="IChatClient"/> that represents the underlying backend.</param>
    public ChatClientBuilder(IChatClient innerClient)
    {
        _ = Throw.IfNull(innerClient);
        _innerClientFactory = _ => innerClient;
    }

    /// <summary>Initializes a new instance of the <see cref="ChatClientBuilder"/> class.</summary>
    /// <param name="innerClientFactory">A callback that produces the inner <see cref="IChatClient"/> that represents the underlying backend.</param>
    public ChatClientBuilder(Func<IServiceProvider, IChatClient> innerClientFactory)
    {
        _innerClientFactory = Throw.IfNull(innerClientFactory);
    }

    /// <summary>Returns an <see cref="IChatClient"/> that represents the entire pipeline. Calls to this instance will pass through each of the pipeline stages in turn.</summary>
    /// <param name="services">
    /// The <see cref="IServiceProvider"/> that should provide services to the <see cref="IChatClient"/> instances.
    /// If null, an empty <see cref="IServiceProvider"/> will be used.
    /// </param>
    /// <returns>An instance of <see cref="IChatClient"/> that represents the entire pipeline.</returns>
    public IChatClient Build(IServiceProvider? services = null)
    {
        services ??= EmptyServiceProvider.Instance;
        var chatClient = _innerClientFactory(services);

        // To match intuitive expectations, apply the factories in reverse order, so that the first factory added is the outermost.
        if (_clientFactories is not null)
        {
            for (var i = _clientFactories.Count - 1; i >= 0; i--)
            {
                chatClient = _clientFactories[i](services, chatClient) ??
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
