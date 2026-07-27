// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides a template for a <see cref="RoutingChatClient"/> that can select another client after an invocation fails.
/// </summary>
/// <remarks>
/// <para>
/// The client for each attempt is supplied by <see cref="RoutingChatClient.SelectClientAsync"/>. After an invocation,
/// <see cref="OnRoutingUpdateAsync"/> reports its outcome and whether routing is terminal. An uncanceled failure causes
/// another selection only when it happened before any streaming output was exposed and the attempt limit permits it.
/// </para>
/// <para>
/// The base class owns invocation, streaming commitment, attempt limits, and terminal reporting. Derived classes own
/// client selection, policy state, and the lifetime of clients they retain.
/// </para>
/// <para>
/// Once streaming enumeration begins, callers must dispose the enumerator. Abandoning an active enumerator without
/// disposing it prevents both inner enumerator disposal and the terminal routing update.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIRoutingChat, UrlFormat = DiagnosticIds.UrlFormat)]
public abstract class FailoverChatClient : RoutingChatClient
{
    /// <summary>Gets or sets the maximum number of client invocations permitted for one request.</summary>
    /// <value>
    /// A positive attempt limit, or <see langword="null"/> to leave termination to client selection and request
    /// cancellation. The default is <see langword="null"/>.
    /// </value>
    /// <remarks>
    /// The value is captured when a non-streaming request begins or when a streaming response begins enumeration.
    /// Changing it does not affect requests or enumerations already in progress.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is not <see langword="null"/> or positive.</exception>
    public int? MaximumAttemptsPerRequest
    {
        get;
        set
        {
            if (value is <= 0)
            {
                Throw.ArgumentOutOfRangeException(nameof(value));
            }

            field = value;
        }
    }

    /// <summary>Invoked after a client invocation or when client selection terminates routing.</summary>
    /// <param name="context">The request-specific inputs.</param>
    /// <param name="attempt">
    /// The completed client invocation, or <see langword="null"/> when <see cref="RoutingChatClient.SelectClientAsync"/>
    /// terminated routing before invoking a client.
    /// </param>
    /// <param name="isTerminal">
    /// <see langword="true"/> if the base will not select another client after this callback completes successfully;
    /// otherwise, <see langword="false"/>.
    /// </param>
    /// <param name="cancellationToken">The cancellation token supplied for the request.</param>
    /// <returns>A task representing the update operation.</returns>
    /// <remarks>
    /// <para>
    /// The default implementation performs no operation. A nonterminal update always contains an uncanceled,
    /// pre-output failed attempt. State changes made by the override are visible to the next call to
    /// <see cref="RoutingChatClient.SelectClientAsync"/>.
    /// </para>
    /// <para>
    /// A <see langword="null"/> attempt is always terminal. If every callback completes successfully, every client
    /// invocation produces one update and exactly one update per request is terminal.
    /// </para>
    /// <para>
    /// Exceptions from this method propagate to the caller. A terminal update exception replaces the response or
    /// exception already produced by the request. A nonterminal update exception stops routing without another update.
    /// An override that retains per-request state must release that state before throwing because no later update is
    /// made after an update exception.
    /// </para>
    /// </remarks>
    protected virtual ValueTask OnRoutingUpdateAsync(
        RoutingContext context,
        FailoverChatClientAttempt? attempt,
        bool isTerminal,
        CancellationToken cancellationToken) => default;

    /// <inheritdoc/>
    public sealed override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var context = new RoutingContext(messages, options);
        int? maximumAttempts = MaximumAttemptsPerRequest;
        int attemptCount = 0;

        while (true)
        {
            IChatClient selectedClient;
            try
            {
                selectedClient = await SelectClientAsync(context, cancellationToken) ??
                    throw new InvalidOperationException($"{nameof(SelectClientAsync)} returned null.");
            }
            catch (Exception)
            {
                await OnRoutingUpdateAsync(context, attempt: null, isTerminal: true, cancellationToken);
                throw;
            }

            attemptCount++;
            ChatResponse? response = null;
            Exception? exception = null;
            long start = Stopwatch.GetTimestamp();

            try
            {
                response = await selectedClient.GetResponseAsync(
                    context.Messages,
                    context.ChatOptions,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            var attempt = new FailoverChatClientAttempt(
                selectedClient,
                exception,
                GetElapsedTime(start),
                timeToFirstUpdate: null,
                responseCompleted: exception is null,
                outputCommitted: false);
            bool isTerminal =
                exception is null ||
                cancellationToken.IsCancellationRequested ||
                (maximumAttempts is int limit && attemptCount >= limit);

            await OnRoutingUpdateAsync(context, attempt, isTerminal, cancellationToken);

            if (exception is null)
            {
                return response!;
            }

            if (isTerminal)
            {
                Rethrow(exception);
            }
        }
    }

    /// <inheritdoc/>
    public sealed override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var context = new RoutingContext(messages, options);
        int? maximumAttempts = MaximumAttemptsPerRequest;
        int attemptCount = 0;

        while (true)
        {
            IChatClient selectedClient;
            try
            {
                selectedClient = await SelectClientAsync(context, cancellationToken) ??
                    throw new InvalidOperationException($"{nameof(SelectClientAsync)} returned null.");
            }
            catch (Exception)
            {
                await OnRoutingUpdateAsync(context, attempt: null, isTerminal: true, cancellationToken);
                throw;
            }

            attemptCount++;
            bool reachedAttemptLimit = maximumAttempts is int limit && attemptCount >= limit;
            IAsyncEnumerator<ChatResponseUpdate>? enumerator = null;
            TimeSpan? timeToFirstUpdate = null;
            TimeSpan activeDuration = TimeSpan.Zero;
            bool hasCurrent;

            long operationStart = Stopwatch.GetTimestamp();
            try
            {
                enumerator = selectedClient
                    .GetStreamingResponseAsync(
                        context.Messages,
                        context.ChatOptions,
                        cancellationToken)
                    .GetAsyncEnumerator(cancellationToken);

                hasCurrent = await enumerator.MoveNextAsync();
            }
            catch (Exception ex)
            {
                activeDuration += GetElapsedTime(operationStart);
                Exception exception = (await DisposeAsync(enumerator, ex, cancellationToken))!;
                var attempt = new FailoverChatClientAttempt(
                    selectedClient,
                    exception,
                    activeDuration,
                    timeToFirstUpdate: null,
                    responseCompleted: false,
                    outputCommitted: false);
                bool isTerminal = cancellationToken.IsCancellationRequested || reachedAttemptLimit;

                await OnRoutingUpdateAsync(context, attempt, isTerminal, cancellationToken);

                if (isTerminal)
                {
                    Rethrow(exception);
                }

                continue;
            }

            activeDuration += GetElapsedTime(operationStart);
            if (hasCurrent)
            {
                timeToFirstUpdate = activeDuration;
            }

            bool responseCompleted = false;
            bool outputCommitted = false;
            bool isTerminalAttempt = false;
            Exception? terminalException = null;

            try
            {
                while (hasCurrent)
                {
                    outputCommitted = true;
                    yield return enumerator.Current;

                    operationStart = Stopwatch.GetTimestamp();
                    try
                    {
                        hasCurrent = await enumerator.MoveNextAsync();
                    }
                    catch (Exception ex)
                    {
                        terminalException = ex;
                        break;
                    }
                    finally
                    {
                        activeDuration += GetElapsedTime(operationStart);
                    }
                }

                responseCompleted = terminalException is null;
            }
            finally
            {
                terminalException = await DisposeAsync(enumerator, terminalException, cancellationToken);

                var attempt = new FailoverChatClientAttempt(
                    selectedClient,
                    terminalException,
                    activeDuration,
                    timeToFirstUpdate,
                    responseCompleted: responseCompleted && terminalException is null,
                    outputCommitted: outputCommitted);
                isTerminalAttempt =
                    attempt.ResponseCompleted ||
                    outputCommitted ||
                    cancellationToken.IsCancellationRequested ||
                    reachedAttemptLimit;

                await OnRoutingUpdateAsync(context, attempt, isTerminalAttempt, cancellationToken);

                if (terminalException is not null && isTerminalAttempt)
                {
                    Rethrow(terminalException);
                }
            }

            if (!isTerminalAttempt)
            {
                continue;
            }

            yield break;
        }
    }

    private static async ValueTask<Exception?> DisposeAsync(
        IAsyncDisposable? disposable,
        Exception? exception,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        if (disposable is not null)
        {
            try
            {
                await disposable.DisposeAsync();
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        return exception;
    }

    private static TimeSpan GetElapsedTime(long startingTimestamp) =>
#if NET
        Stopwatch.GetElapsedTime(startingTimestamp);
#else
        new((long)((Stopwatch.GetTimestamp() - startingTimestamp) *
            ((double)TimeSpan.TicksPerSecond / Stopwatch.Frequency)));
#endif

    [DoesNotReturn]
    private static void Rethrow(Exception exception)
    {
        ExceptionDispatchInfo.Capture(exception).Throw();
        throw exception;
    }

}
