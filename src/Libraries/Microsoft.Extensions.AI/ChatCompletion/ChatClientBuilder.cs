// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>A builder for creating pipelines of <see cref="IChatClient"/>.</summary>
public sealed class ChatClientBuilder
{
    private readonly Func<IServiceProvider, IChatClient> _innerClientFactory;

    /// <summary>The registered client factory instances.</summary>
    private List<Func<IChatClient, IServiceProvider, IChatClient>>? _clientFactories;

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

    /// <summary>Builds an <see cref="IChatClient"/> that represents the entire pipeline. Calls to this instance will pass through each of the pipeline stages in turn.</summary>
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
                chatClient = _clientFactories[i](chatClient, services) ??
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

        return Use((innerClient, _) => clientFactory(innerClient));
    }

    /// <summary>Adds a factory for an intermediate chat client to the chat client pipeline.</summary>
    /// <param name="clientFactory">The client factory function.</param>
    /// <returns>The updated <see cref="ChatClientBuilder"/> instance.</returns>
    public ChatClientBuilder Use(Func<IChatClient, IServiceProvider, IChatClient> clientFactory)
    {
        _ = Throw.IfNull(clientFactory);

        (_clientFactories ??= []).Add(clientFactory);
        return this;
    }

    /// <summary>
    /// Adds to the chat client pipeline an anonymous delegating chat client based on a delegate that provides
    /// an implementation for both <see cref="IChatClient.GetResponseAsync"/> and <see cref="IChatClient.GetStreamingResponseAsync"/>.
    /// </summary>
    /// <param name="sharedFunc">
    /// A delegate that provides the implementation for both <see cref="IChatClient.GetResponseAsync"/> and
    /// <see cref="IChatClient.GetStreamingResponseAsync"/>. In addition to the arguments for the operation, it's
    /// provided with a delegate to the inner client that should be used to perform the operation on the inner client.
    /// It will handle both the non-streaming and streaming cases.
    /// </param>
    /// <returns>The updated <see cref="ChatClientBuilder"/> instance.</returns>
    /// <remarks>
    /// This overload may be used when the anonymous implementation needs to provide pre- and/or post-processing, but doesn't
    /// need to interact with the results of the operation, which will come from the inner client.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="sharedFunc"/> is <see langword="null"/>.</exception>
    public ChatClientBuilder Use(AnonymousDelegatingChatClient.GetResponseSharedFunc sharedFunc)
    {
        _ = Throw.IfNull(sharedFunc);

        return Use((innerClient, _) => new AnonymousDelegatingChatClient(innerClient, sharedFunc));
    }

    /// <summary>
    /// Adds to the chat client pipeline an anonymous delegating chat client based on a delegate that provides
    /// an implementation for both <see cref="IChatClient.GetResponseAsync"/> and <see cref="IChatClient.GetStreamingResponseAsync"/>.
    /// </summary>
    /// <param name="getResponseFunc">
    /// A delegate that provides the implementation for <see cref="IChatClient.GetResponseAsync"/>. When <see langword="null"/>,
    /// <paramref name="getStreamingResponseFunc"/> must be non-null, and the implementation of <see cref="IChatClient.GetResponseAsync"/>
    /// will use <paramref name="getStreamingResponseFunc"/> for the implementation.
    /// </param>
    /// <param name="getStreamingResponseFunc">
    /// A delegate that provides the implementation for <see cref="IChatClient.GetStreamingResponseAsync"/>. When <see langword="null"/>,
    /// <paramref name="getResponseFunc"/> must be non-null, and the implementation of <see cref="IChatClient.GetStreamingResponseAsync"/>
    /// will use <paramref name="getResponseFunc"/> for the implementation.
    /// </param>
    /// <returns>The updated <see cref="ChatClientBuilder"/> instance.</returns>
    /// <remarks>
    /// One or both delegates may be provided. If both are provided, they will be used for their respective methods:
    /// <paramref name="getResponseFunc"/> will provide the implementation of <see cref="IChatClient.GetResponseAsync"/>, and
    /// <paramref name="getStreamingResponseFunc"/> will provide the implementation of <see cref="IChatClient.GetStreamingResponseAsync"/>.
    /// If only one of the delegates is provided, it will be used for both methods. That means that if <paramref name="getResponseFunc"/>
    /// is supplied without <paramref name="getStreamingResponseFunc"/>, the implementation of <see cref="IChatClient.GetStreamingResponseAsync"/>
    /// will employ limited streaming, as it will be operating on the batch output produced by <paramref name="getResponseFunc"/>. And if
    /// <paramref name="getStreamingResponseFunc"/> is supplied without <paramref name="getResponseFunc"/>, the implementation of
    /// <see cref="IChatClient.GetResponseAsync"/> will be implemented by combining the updates from <paramref name="getStreamingResponseFunc"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Both <paramref name="getResponseFunc"/> and <paramref name="getStreamingResponseFunc"/> are <see langword="null"/>.</exception>
    public ChatClientBuilder Use(
        Func<IList<ChatMessage>, ChatOptions?, IChatClient, CancellationToken, Task<ChatResponse>>? getResponseFunc,
        Func<IList<ChatMessage>, ChatOptions?, IChatClient, CancellationToken, IAsyncEnumerable<ChatResponseUpdate>>? getStreamingResponseFunc)
    {
        AnonymousDelegatingChatClient.ThrowIfBothDelegatesNull(getResponseFunc, getStreamingResponseFunc);

        return Use((innerClient, _) => new AnonymousDelegatingChatClient(innerClient, getResponseFunc, getStreamingResponseFunc));
    }
}
