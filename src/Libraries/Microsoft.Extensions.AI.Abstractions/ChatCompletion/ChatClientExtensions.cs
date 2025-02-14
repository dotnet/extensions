﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides a collection of static methods for extending <see cref="IChatClient"/> instances.</summary>
public static class ChatClientExtensions
{
    /// <summary>Asks the <see cref="IChatClient"/> for an object of type <typeparamref name="TService"/>.</summary>
    /// <typeparam name="TService">The type of the object to be retrieved.</typeparam>
    /// <param name="client">The client.</param>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object, otherwise <see langword="null"/>.</returns>
    /// <remarks>
    /// The purpose of this method is to allow for the retrieval of strongly typed services that may be provided by the <see cref="IChatClient"/>,
    /// including itself or any services it might be wrapping.
    /// </remarks>
    public static TService? GetService<TService>(this IChatClient client, object? serviceKey = null)
    {
        _ = Throw.IfNull(client);

        return (TService?)client.GetService(typeof(TService), serviceKey);
    }

    /// <summary>Sends a user chat text message and returns the response messages.</summary>
    /// <param name="client">The chat client.</param>
    /// <param name="chatMessage">The text content for the chat message to send.</param>
    /// <param name="options">The chat options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The response messages generated by the client.</returns>
    public static Task<ChatResponse> GetResponseAsync(
        this IChatClient client,
        string chatMessage,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(client);
        _ = Throw.IfNull(chatMessage);

        return client.GetResponseAsync(new ChatMessage(ChatRole.User, chatMessage), options, cancellationToken);
    }

    /// <summary>Sends a chat message and returns the response messages.</summary>
    /// <param name="client">The chat client.</param>
    /// <param name="chatMessage">The chat message to send.</param>
    /// <param name="options">The chat options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The response messages generated by the client.</returns>
    public static Task<ChatResponse> GetResponseAsync(
        this IChatClient client,
        ChatMessage chatMessage,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(client);
        _ = Throw.IfNull(chatMessage);

        return client.GetResponseAsync([chatMessage], options, cancellationToken);
    }

    /// <summary>Sends a user chat text message and streams the response messages.</summary>
    /// <param name="client">The chat client.</param>
    /// <param name="chatMessage">The text content for the chat message to send.</param>
    /// <param name="options">The chat options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The response messages generated by the client.</returns>
    public static IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        this IChatClient client,
        string chatMessage,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(client);
        _ = Throw.IfNull(chatMessage);

        return client.GetStreamingResponseAsync(new ChatMessage(ChatRole.User, chatMessage), options, cancellationToken);
    }

    /// <summary>Sends a chat message and streams the response messages.</summary>
    /// <param name="client">The chat client.</param>
    /// <param name="chatMessage">The chat message to send.</param>
    /// <param name="options">The chat options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The response messages generated by the client.</returns>
    public static IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        this IChatClient client,
        ChatMessage chatMessage,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(client);
        _ = Throw.IfNull(chatMessage);

        return client.GetStreamingResponseAsync([chatMessage], options, cancellationToken);
    }
}
