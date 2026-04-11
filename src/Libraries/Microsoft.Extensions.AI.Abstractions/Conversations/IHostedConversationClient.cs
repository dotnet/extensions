// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a client for managing hosted conversations.</summary>
/// <remarks>
/// <para>
/// Unless otherwise specified, all members of <see cref="IHostedConversationClient"/> are thread-safe for concurrent use.
/// It is expected that all implementations of <see cref="IHostedConversationClient"/> support being used by multiple requests concurrently.
/// </para>
/// <para>
/// However, implementations of <see cref="IHostedConversationClient"/> might mutate the arguments supplied to
/// <see cref="CreateAsync"/> and <see cref="AddMessagesAsync"/>, such as by configuring the options or messages instances.
/// Thus, consumers of the interface either should avoid using shared instances of these arguments for concurrent
/// invocations or should otherwise ensure by construction that no <see cref="IHostedConversationClient"/> instances
/// are used which might employ such mutation.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIHostedConversation, UrlFormat = DiagnosticIds.UrlFormat)]
public interface IHostedConversationClient : IDisposable
{
    /// <summary>Creates a new hosted conversation.</summary>
    /// <param name="options">The options to configure the conversation creation.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The created <see cref="HostedConversation"/>.</returns>
    Task<HostedConversation> CreateAsync(
        HostedConversationClientOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Retrieves an existing hosted conversation by its identifier.</summary>
    /// <param name="conversationId">The unique identifier of the conversation to retrieve.</param>
    /// <param name="options">The options for the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The <see cref="HostedConversation"/> matching the specified <paramref name="conversationId"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="conversationId"/> is <see langword="null"/>.</exception>
    Task<HostedConversation> GetAsync(
        string conversationId,
        HostedConversationClientOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes an existing hosted conversation.</summary>
    /// <param name="conversationId">The unique identifier of the conversation to delete.</param>
    /// <param name="options">The options for the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="conversationId"/> is <see langword="null"/>.</exception>
    Task DeleteAsync(
        string conversationId,
        HostedConversationClientOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Adds messages to an existing hosted conversation.</summary>
    /// <param name="conversationId">The unique identifier of the conversation to add messages to.</param>
    /// <param name="messages">The sequence of chat messages to add to the conversation.</param>
    /// <param name="options">The options for the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="conversationId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="messages"/> is <see langword="null"/>.</exception>
    Task AddMessagesAsync(
        string conversationId,
        IEnumerable<ChatMessage> messages,
        HostedConversationClientOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Lists the messages in an existing hosted conversation.</summary>
    /// <param name="conversationId">The unique identifier of the conversation to list messages from.</param>
    /// <param name="options">The options for the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>An asynchronous sequence of <see cref="ChatMessage"/> instances from the conversation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="conversationId"/> is <see langword="null"/>.</exception>
    IAsyncEnumerable<ChatMessage> GetMessagesAsync(
        string conversationId,
        HostedConversationClientOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Lists hosted conversations accessible by this client.</summary>
    /// <param name="options">The options for the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>An asynchronous sequence of <see cref="HostedConversation"/> instances.</returns>
    IAsyncEnumerable<HostedConversation> ListConversationsAsync(
        HostedConversationClientOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Asks the <see cref="IHostedConversationClient"/> for an object of the specified type <paramref name="serviceType"/>.</summary>
    /// <param name="serviceType">The type of object being requested.</param>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object, otherwise <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceType"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The purpose of this method is to allow for the retrieval of strongly typed services that might be provided by the <see cref="IHostedConversationClient"/>,
    /// including itself or any services it might be wrapping.
    /// </remarks>
    object? GetService(Type serviceType, object? serviceKey = null);
}
