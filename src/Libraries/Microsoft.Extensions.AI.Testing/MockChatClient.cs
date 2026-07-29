// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// A deterministic <see cref="IChatClient"/> implementation for tests and local mock scenarios,
/// using seeded responses based on request predicates.
/// </summary>
/// <remarks>
/// <para>
/// This client starts with no seeded responses. Each request is matched against seeded responses
/// in reverse insertion order, and the most recently added match is used.
/// </para>
/// <para>
/// Seeds are reusable by default. Set <c>singleUse: true</c> to remove a seed after its first match.
/// </para>
/// <para>
/// If no seed matches a request, an <see cref="InvalidOperationException"/> is thrown. Response factories
/// can return fully populated <see cref="ChatResponse"/> and <see cref="ChatResponseUpdate"/> payloads to model
/// citations, reasoning, tool calls, usage, metadata, errors, and other chat-client behavior.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AITesting, UrlFormat = DiagnosticIds.UrlFormat)]
public partial class MockChatClient : IChatClient
{
    private static ChatResponse CloneResponse(ChatResponse response)
    {
        var clone = new ChatResponse(response.Messages.Select(message => message.Clone()).ToList())
        {
            AdditionalProperties = response.AdditionalProperties?.Clone(),
            ContinuationToken = response.ContinuationToken,
            ConversationId = response.ConversationId,
            CreatedAt = response.CreatedAt,
            FinishReason = response.FinishReason,
            ModelId = response.ModelId,
            RawRepresentation = response.RawRepresentation,
            ResponseId = response.ResponseId,
            Usage = response.Usage,
        };

        return clone;
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> EnumerateUpdatesAsync(
        IEnumerable<ChatResponseUpdate> updates,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (ChatResponseUpdate update in updates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return Throw.IfNull(update).Clone();
        }
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> EnumerateUpdatesAsync(
        IAsyncEnumerable<ChatResponseUpdate> updates,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (ChatResponseUpdate update in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return Throw.IfNull(update).Clone();
        }
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> GetResponseUpdatesAsync(
        Func<MockChatClientRequest, CancellationToken, Task<ChatResponse>> getResponse,
        MockChatClientRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ChatResponse response = Throw.IfNull(await getResponse(request, cancellationToken).ConfigureAwait(false));
        await foreach (ChatResponseUpdate update in EnumerateUpdatesAsync(response.ToChatResponseUpdates(), cancellationToken).ConfigureAwait(false))
        {
            yield return update;
        }
    }

    private static Task<ChatResponse> GetResponseFromUpdatesAsync(
        Func<MockChatClientRequest, CancellationToken, IAsyncEnumerable<ChatResponseUpdate>> getResponse,
        MockChatClientRequest request,
        CancellationToken cancellationToken) =>
        EnumerateUpdatesAsync(Throw.IfNull(getResponse(request, cancellationToken)), cancellationToken)
            .ToChatResponseAsync(cancellationToken);

    private static async IAsyncEnumerable<ChatResponseUpdate> ThrowExceptionAsync(
        Func<Exception> exceptionFactory,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.FromException(Throw.IfNull(exceptionFactory())).ConfigureAwait(false);
        yield break;
    }

    private readonly object _sync = new();
    private readonly List<SeededResponse> _seededResponses = [];
    private readonly List<MockChatClientRequest> _requests = [];
    private bool _disposed;

    /// <summary>
    /// Gets or sets an optional service provider surfaced through <see cref="GetService"/>.
    /// </summary>
    /// <remarks>
    /// This provider is queried only for non-keyed lookups when the requested service type is not
    /// <see cref="MockChatClient"/> itself.
    /// </remarks>
    public IServiceProvider? Services { get; set; }

    /// <summary>
    /// Gets a snapshot of requests previously sent to this instance.
    /// </summary>
    /// <remarks>
    /// Requests are recorded in call order across both streaming and non-streaming APIs.
    /// </remarks>
    public IReadOnlyList<MockChatClientRequest> Requests
    {
        get
        {
            lock (_sync)
            {
                return _requests.ToArray();
            }
        }
    }

    /// <summary>Seeds a response factory for matching requests.</summary>
    /// <param name="requestPredicate">Predicate used to select whether this seed applies to a request.</param>
    /// <param name="getResponse">
    /// Asynchronously creates the response for a matching request. The supplied cancellation token is the one passed
    /// to the matching chat-client call.
    /// </param>
    /// <param name="singleUse">
    /// <see langword="true"/> to remove this seed after its first matching request; otherwise it remains reusable.
    /// </param>
    /// <returns>The same <see cref="MockChatClient"/> instance for chaining.</returns>
    /// <remarks>
    /// If <see cref="GetStreamingResponseAsync(IEnumerable{ChatMessage}, ChatOptions?, CancellationToken)"/> is called
    /// for a matching request, the response is converted to updates by using the chat response update conversion helpers.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="requestPredicate"/> or <paramref name="getResponse"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    public virtual MockChatClient AddResponse(
        Func<MockChatClientRequest, bool> requestPredicate,
        Func<MockChatClientRequest, CancellationToken, Task<ChatResponse>> getResponse,
        bool singleUse = false) =>
        AddSeed(
            requestPredicate,
            getResponse,
            (request, cancellationToken) => GetResponseUpdatesAsync(getResponse, request, cancellationToken),
            singleUse);

    /// <summary>Seeds non-streaming and streaming response factories for matching requests.</summary>
    /// <param name="requestPredicate">Predicate used to select whether this seed applies to a request.</param>
    /// <param name="getResponse">
    /// Asynchronously creates the response for a matching non-streaming request. The supplied cancellation token is
    /// the one passed to the matching chat-client call.
    /// </param>
    /// <param name="getStreamingResponse">
    /// Asynchronously creates the updates for a matching streaming request. The supplied cancellation token is the
    /// one passed to the matching chat-client call.
    /// </param>
    /// <param name="singleUse">
    /// <see langword="true"/> to remove this seed after its first matching request; otherwise it remains reusable.
    /// </param>
    /// <returns>The same <see cref="MockChatClient"/> instance for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="requestPredicate"/>, <paramref name="getResponse"/>, or
    /// <paramref name="getStreamingResponse"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    public virtual MockChatClient AddResponse(
        Func<MockChatClientRequest, bool> requestPredicate,
        Func<MockChatClientRequest, CancellationToken, Task<ChatResponse>> getResponse,
        Func<MockChatClientRequest, CancellationToken, IAsyncEnumerable<ChatResponseUpdate>> getStreamingResponse,
        bool singleUse = false) =>
        AddSeed(requestPredicate, getResponse, getStreamingResponse, singleUse);

    /// <summary>Seeds a streaming response factory for matching requests.</summary>
    /// <param name="requestPredicate">Predicate used to select whether this seed applies to a request.</param>
    /// <param name="getResponse">
    /// Asynchronously creates the updates for a matching streaming request. The supplied cancellation token is the
    /// one passed to the matching chat-client call.
    /// </param>
    /// <param name="singleUse">
    /// <see langword="true"/> to remove this seed after its first matching request; otherwise it remains reusable.
    /// </param>
    /// <returns>The same <see cref="MockChatClient"/> instance for chaining.</returns>
    /// <remarks>
    /// If <see cref="GetResponseAsync(IEnumerable{ChatMessage}, ChatOptions?, CancellationToken)"/> is called for a
    /// matching request, these updates are converted to a single response by using the chat response conversion helpers.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="requestPredicate"/> or <paramref name="getResponse"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    public virtual MockChatClient AddStreamingResponse(
        Func<MockChatClientRequest, bool> requestPredicate,
        Func<MockChatClientRequest, CancellationToken, IAsyncEnumerable<ChatResponseUpdate>> getResponse,
        bool singleUse = false) =>
        AddSeed(
            requestPredicate,
            (request, cancellationToken) => GetResponseFromUpdatesAsync(getResponse, request, cancellationToken),
            getResponse,
            singleUse);

    /// <summary>Seeds an exception for matching requests.</summary>
    /// <param name="requestPredicate">Predicate used to select whether this seed applies to a request.</param>
    /// <param name="exceptionFactory">Creates the exception to throw for each matching request.</param>
    /// <param name="singleUse">
    /// <see langword="true"/> to remove this seed after its first matching request; otherwise it remains reusable.
    /// </param>
    /// <returns>The same <see cref="MockChatClient"/> instance for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="requestPredicate"/> or <paramref name="exceptionFactory"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    public virtual MockChatClient AddException(
        Func<MockChatClientRequest, bool> requestPredicate,
        Func<Exception> exceptionFactory,
        bool singleUse = false)
    {
        _ = Throw.IfNull(exceptionFactory);

        return AddSeed(
            requestPredicate,
            (_, _) => Task.FromException<ChatResponse>(Throw.IfNull(exceptionFactory())),
            (_, cancellationToken) => ThrowExceptionAsync(exceptionFactory, cancellationToken),
            singleUse);
    }

    /// <summary>Removes all seeded responses and exceptions.</summary>
    /// <remarks>
    /// This method does not clear <see cref="Requests"/>.
    /// </remarks>
    /// <returns>The same <see cref="MockChatClient"/> instance for chaining.</returns>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    public virtual MockChatClient ClearResponses()
    {
        ThrowIfDisposed();

        lock (_sync)
        {
            _seededResponses.Clear();
        }

        return this;
    }

    /// <summary>
    /// Returns a response for the most recently seeded match and records the request.
    /// </summary>
    /// <param name="messages">The request messages.</param>
    /// <param name="options">Request options, if any.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A deterministic response for the most recently seeded match.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="messages"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No seeded response matched the request.</exception>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    public virtual async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);
        ThrowIfDisposed();

        MockChatClientRequest request = RecordRequest(messages, options, isStreaming: false);
        SeededResponse seeded = MatchSeededResponse(request);
        return CloneResponse(Throw.IfNull(await seeded.ResponseFactory(request, cancellationToken).ConfigureAwait(false)));
    }

    /// <summary>
    /// Returns streaming updates for the most recently seeded match and records the request.
    /// </summary>
    /// <param name="messages">The request messages.</param>
    /// <param name="options">Request options, if any.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A deterministic update stream for the most recently seeded match.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="messages"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No seeded response matched the request.</exception>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    public virtual IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);
        ThrowIfDisposed();

        MockChatClientRequest request = RecordRequest(messages, options, isStreaming: true);
        SeededResponse seeded = MatchSeededResponse(request);
        return EnumerateUpdatesAsync(Throw.IfNull(seeded.StreamingFactory(request, cancellationToken)), cancellationToken);
    }

    /// <summary>
    /// Gets a service object from this client or the optional <see cref="Services"/> provider.
    /// </summary>
    /// <param name="serviceType">The type of service object to get.</param>
    /// <param name="serviceKey">An optional service key. Keyed lookup is not supported.</param>
    /// <returns>
    /// This instance when <paramref name="serviceType"/> is assignable from <see cref="MockChatClient"/>;
    /// otherwise a non-keyed service from <see cref="Services"/> when available; otherwise <see langword="null"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceType"/> is <see langword="null"/>.</exception>
    public virtual object? GetService(Type serviceType, object? serviceKey = null)
    {
        _ = Throw.IfNull(serviceType);

        if (serviceKey is null)
        {
            if (serviceType.IsInstanceOfType(this))
            {
                return this;
            }

            return Services?.GetService(serviceType);
        }

        return null;
    }

    /// <summary>Disposes this instance and clears all seeds and recorded requests.</summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Adds a response seed.</summary>
    /// <param name="requestPredicate">Predicate used to select whether this seed applies to a request.</param>
    /// <param name="getResponse">Factory that creates a response for a matching request.</param>
    /// <param name="getStreamingResponse">Factory that creates streaming updates for a matching request.</param>
    /// <param name="singleUse">
    /// <see langword="true"/> to remove this seed after its first matching request; otherwise it remains reusable.
    /// </param>
    /// <returns>The same <see cref="MockChatClient"/> instance for chaining.</returns>
    protected virtual MockChatClient AddSeed(
        Func<MockChatClientRequest, bool> requestPredicate,
        Func<MockChatClientRequest, CancellationToken, Task<ChatResponse>> getResponse,
        Func<MockChatClientRequest, CancellationToken, IAsyncEnumerable<ChatResponseUpdate>> getStreamingResponse,
        bool singleUse)
    {
        _ = Throw.IfNull(requestPredicate);
        _ = Throw.IfNull(getResponse);
        _ = Throw.IfNull(getStreamingResponse);

        lock (_sync)
        {
            ThrowIfDisposed();

            _seededResponses.Add(new SeededResponse
            {
                RequestPredicate = requestPredicate,
                ResponseFactory = getResponse,
                StreamingFactory = getStreamingResponse,
                SingleUse = singleUse,
            });
        }

        return this;
    }

    /// <summary>Finds the most recently added seed that matches a request.</summary>
    /// <param name="request">The request to match.</param>
    /// <returns>The matching seed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No seeded response matched the request.</exception>
    protected virtual SeededResponse MatchSeededResponse(MockChatClientRequest request)
    {
        request = Throw.IfNull(request);

        lock (_sync)
        {
            for (int i = _seededResponses.Count - 1; i >= 0; i--)
            {
                SeededResponse seeded = _seededResponses[i];
                if (!seeded.RequestPredicate(request))
                {
                    continue;
                }

                if (seeded.SingleUse)
                {
                    _seededResponses.RemoveAt(i);
                }

                return seeded;
            }
        }

        throw new InvalidOperationException(
            $"No seeded response matched. Last user text: '{request.LastUserText ?? "<none>"}'.");
    }

    /// <summary>Represents a response seed.</summary>
    protected sealed class SeededResponse
    {
        /// <summary>Gets the predicate used to select whether this seed applies to a request.</summary>
        public Func<MockChatClientRequest, bool> RequestPredicate { get; init; } = default!;

        /// <summary>Gets the factory that creates a response for a matching request.</summary>
        public Func<MockChatClientRequest, CancellationToken, Task<ChatResponse>> ResponseFactory { get; init; } = default!;

        /// <summary>Gets the factory that creates streaming updates for a matching request.</summary>
        public Func<MockChatClientRequest, CancellationToken, IAsyncEnumerable<ChatResponseUpdate>> StreamingFactory { get; init; } = default!;

        /// <summary>Gets a value indicating whether this seed is removed after its first matching request.</summary>
        public bool SingleUse { get; init; }
    }

    /// <summary>Releases resources used by this mock client.</summary>
    /// <param name="disposing"><see langword="true"/> when called from <see cref="Dispose()"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        lock (_sync)
        {
            _disposed = true;
            _seededResponses.Clear();
            _requests.Clear();
        }
    }

    private MockChatClientRequest RecordRequest(IEnumerable<ChatMessage> messages, ChatOptions? options, bool isStreaming)
    {
        ChatMessage[] messageArray = messages.Select(m => Throw.IfNull(m).Clone()).ToArray();
        var request = new MockChatClientRequest(messageArray, options, isStreaming);

        lock (_sync)
        {
            _requests.Add(request);
        }

        return request;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MockChatClient));
        }
    }
}
