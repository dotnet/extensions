﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a chat client.</summary>
/// <remarks>
/// <para>
/// Unless otherwise specified, all members of <see cref="IChatClient"/> are thread-safe for concurrent use.
/// It is expected that all implementations of <see cref="IChatClient"/> support being used by multiple requests concurrently.
/// Instances must not be disposed of while the instance is still in use.
/// </para>
/// <para>
/// However, implementations of <see cref="IChatClient"/> might mutate the arguments supplied to <see cref="GetResponseAsync"/> and
/// <see cref="GetStreamingResponseAsync"/>, such as by configuring the options instance. Thus, consumers of the interface either
/// should avoid using shared instances of these arguments for concurrent invocations or should otherwise ensure by construction
/// that no <see cref="IChatClient"/> instances are used which might employ such mutation. For example, the ConfigureOptions method is
/// provided with a callback that could mutate the supplied options argument, and that should be avoided if using a singleton options instance.
/// </para>
/// </remarks>
/// <related type="Article" href="https://learn.microsoft.com/dotnet/ai/quickstarts/build-chat-app">Build an AI chat app with .NET.</related>
public interface IChatClient : IDisposable
{
    /// <summary>Sends chat messages and returns the response.</summary>
    /// <param name="messages">The sequence of chat messages to send.</param>
    /// <param name="options">The chat options with which to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The response messages generated by the client.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="messages"/> is <see langword="null"/>.</exception>
    Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Sends chat messages and streams the response.</summary>
    /// <param name="messages">The sequence of chat messages to send.</param>
    /// <param name="options">The chat options with which to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The response messages generated by the client.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="messages"/> is <see langword="null"/>.</exception>
    IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Asks the <see cref="IChatClient"/> for an object of the specified type <paramref name="serviceType"/>.</summary>
    /// <param name="serviceType">The type of object being requested.</param>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object, otherwise <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceType"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The purpose of this method is to allow for the retrieval of strongly-typed services that might be provided by the <see cref="IChatClient"/>,
    /// including itself or any services it might be wrapping. For example, to access the <see cref="ChatClientMetadata"/> for the instance,
    /// <see cref="GetService"/> may be used to request it.
    /// </remarks>
    object? GetService(Type serviceType, object? serviceKey = null);
}
