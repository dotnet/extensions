// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides ordered failover across a sequence of chat clients.</summary>
/// <remarks>
/// <para>
/// The clients are tried in order. An invocation failure before streaming output is exposed advances to the next
/// client. Cancellation and failures after streaming output is exposed are propagated without failover.
/// </para>
/// <para>
/// The configured clients are snapshotted by the constructor and must contain unique object references. When every
/// client has failed, the final failure is rethrown.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIRoutingChat, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class OrderedFailoverChatClient : FailoverChatClient
{
    private readonly bool _leaveOpen;
    private readonly IChatClient[] _clients;
    private readonly ConcurrentDictionary<RoutingContext, RequestState> _requestStates = new();
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="OrderedFailoverChatClient"/> class.</summary>
    /// <param name="clients">The clients to invoke, in fallback order.</param>
    /// <param name="leaveOpen">
    /// <see langword="true"/> to leave inner clients open when this instance is disposed;
    /// otherwise, <see langword="false"/>.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="clients"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="clients"/> is empty, contains <see langword="null"/>, or contains the same client instance more
    /// than once.
    /// </exception>
    public OrderedFailoverChatClient(IReadOnlyList<IChatClient> clients, bool leaveOpen = false)
    {
        _ = Throw.IfNull(clients);

        IChatClient[] clientsSnapshot = [.. clients];
        if (clientsSnapshot.Length == 0)
        {
            Throw.ArgumentException(nameof(clients), "At least one client must be provided.");
        }

        for (int i = 0; i < clientsSnapshot.Length; i++)
        {
            if (clientsSnapshot[i] is null)
            {
                Throw.ArgumentException(nameof(clients), "Clients must not contain null.");
            }

            for (int j = 0; j < i; j++)
            {
                if (ReferenceEquals(clientsSnapshot[j], clientsSnapshot[i]))
                {
                    Throw.ArgumentException(nameof(clients), "Each client instance must be unique.");
                }
            }
        }

        _leaveOpen = leaveOpen;
        _clients = clientsSnapshot;
    }

    /// <inheritdoc/>
    protected override ValueTask<IChatClient> SelectClientAsync(
        RoutingContext context,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        if (!_requestStates.TryRemove(context, out RequestState? state))
        {
            return new(_clients[0]);
        }

        if (state.ClientIndex < _clients.Length)
        {
            return new(_clients[state.ClientIndex]);
        }

        ExceptionDispatchInfo.Capture(state.LastException).Throw();
        throw state.LastException;
    }

    /// <inheritdoc/>
    protected override ValueTask OnRoutingUpdateAsync(
        RoutingContext context,
        FailoverChatClientAttempt attempt,
        bool isTerminal,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        if (isTerminal)
        {
            _ = _requestStates.TryRemove(context, out _);
            return default;
        }

        if (attempt.Exception is null)
        {
            _ = _requestStates.TryRemove(context, out _);
            throw new InvalidOperationException("A nonterminal routing update requires a failed client invocation.");
        }

        int clientIndex = IndexOfClient(attempt.Client);
        if (clientIndex < 0)
        {
            _ = _requestStates.TryRemove(context, out _);
            throw new InvalidOperationException("The invocation did not use a configured ordered failover client.");
        }

        // Selection immediately removes this state before invoking the next client.
        _requestStates[context] = new(clientIndex + 1, attempt.Exception);

        return default;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _requestStates.Clear();

        if (disposing && !_leaveOpen)
        {
            foreach (IChatClient client in _clients)
            {
                client.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private int IndexOfClient(IChatClient client)
    {
        for (int i = 0; i < _clients.Length; i++)
        {
            if (ReferenceEquals(_clients[i], client))
            {
                return i;
            }
        }

        return -1;
    }

    private sealed class RequestState(int clientIndex, Exception lastException)
    {
        public int ClientIndex { get; } = clientIndex;

        public Exception LastException { get; } = lastException;
    }
}
