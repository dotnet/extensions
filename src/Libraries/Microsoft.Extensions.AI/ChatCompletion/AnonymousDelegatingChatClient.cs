// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks

namespace Microsoft.Extensions.AI;

/// <summary>A delegating chat client that wraps an inner client with implementations provided by delegates.</summary>
public sealed class AnonymousDelegatingChatClient : DelegatingChatClient
{
    /// <summary>The delegate to use as the implementation of <see cref="CompleteAsync"/>.</summary>
    private readonly Func<IList<ChatMessage>, ChatOptions?, IChatClient, CancellationToken, Task<ChatCompletion>>? _completeFunc;

    /// <summary>The delegate to use as the implementation of <see cref="CompleteStreamingAsync"/>.</summary>
    /// <remarks>
    /// When non-<see langword="null"/>, this delegate is used as the implementation of <see cref="CompleteStreamingAsync"/> and
    /// will be invoked with the same arguments as the method itself, along with a reference to the inner client.
    /// When <see langword="null"/>, <see cref="CompleteStreamingAsync"/> will delegate directly to the inner client.
    /// </remarks>
    private readonly Func<IList<ChatMessage>, ChatOptions?, IChatClient, CancellationToken, IAsyncEnumerable<StreamingChatCompletionUpdate>>? _completeStreamingFunc;

    /// <summary>The delegate to use as the implementation of both <see cref="CompleteAsync"/> and <see cref="CompleteStreamingAsync"/>.</summary>
    private readonly CompleteSharedFunc? _sharedFunc;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnonymousDelegatingChatClient"/> class.
    /// </summary>
    /// <param name="innerClient">The inner client.</param>
    /// <param name="sharedFunc">
    /// A delegate that provides the implementation for both <see cref="CompleteAsync"/> and <see cref="CompleteStreamingAsync"/>.
    /// In addition to the arguments for the operation, it's provided with a delegate to the inner client that should be
    /// used to perform the operation on the inner client. It will handle both the non-streaming and streaming cases.
    /// </param>
    /// <remarks>
    /// This overload may be used when the anonymous implementation needs to provide pre- and/or post-processing, but doesn't
    /// need to interact with the results of the operation, which will come from the inner client.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="innerClient"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="sharedFunc"/> is <see langword="null"/>.</exception>
    public AnonymousDelegatingChatClient(IChatClient innerClient, CompleteSharedFunc sharedFunc)
        : base(innerClient)
    {
        _ = Throw.IfNull(sharedFunc);

        _sharedFunc = sharedFunc;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AnonymousDelegatingChatClient"/> class.
    /// </summary>
    /// <param name="innerClient">The inner client.</param>
    /// <param name="completeFunc">
    /// A delegate that provides the implementation for <see cref="CompleteAsync"/>. When <see langword="null"/>,
    /// <paramref name="completeStreamingFunc"/> must be non-null, and the implementation of <see cref="CompleteAsync"/>
    /// will use <paramref name="completeStreamingFunc"/> for the implementation.
    /// </param>
    /// <param name="completeStreamingFunc">
    /// A delegate that provides the implementation for <see cref="CompleteStreamingAsync"/>. When <see langword="null"/>,
    /// <paramref name="completeFunc"/> must be non-null, and the implementation of <see cref="CompleteStreamingAsync"/>
    /// will use <paramref name="completeFunc"/> for the implementation.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="innerClient"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException">Both <paramref name="completeFunc"/> and <paramref name="completeStreamingFunc"/> are <see langword="null"/>.</exception>
    public AnonymousDelegatingChatClient(
        IChatClient innerClient,
        Func<IList<ChatMessage>, ChatOptions?, IChatClient, CancellationToken, Task<ChatCompletion>>? completeFunc,
        Func<IList<ChatMessage>, ChatOptions?, IChatClient, CancellationToken, IAsyncEnumerable<StreamingChatCompletionUpdate>>? completeStreamingFunc)
        : base(innerClient)
    {
        ThrowIfBothDelegatesNull(completeFunc, completeStreamingFunc);

        _completeFunc = completeFunc;
        _completeStreamingFunc = completeStreamingFunc;
    }

    /// <inheritdoc/>
    public override Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(chatMessages);

        if (_sharedFunc is not null)
        {
            return CompleteViaSharedAsync(chatMessages, options, cancellationToken);

            async Task<ChatCompletion> CompleteViaSharedAsync(IList<ChatMessage> chatMessages, ChatOptions? options, CancellationToken cancellationToken)
            {
                ChatCompletion? completion = null;
                await _sharedFunc(chatMessages, options, async (chatMessages, options, cancellationToken) =>
                {
                    completion = await InnerClient.CompleteAsync(chatMessages, options, cancellationToken).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);

                if (completion is null)
                {
                    throw new InvalidOperationException("The wrapper completed successfully without producing a ChatCompletion.");
                }

                return completion;
            }
        }
        else if (_completeFunc is not null)
        {
            return _completeFunc(chatMessages, options, InnerClient, cancellationToken);
        }
        else
        {
            Debug.Assert(_completeStreamingFunc is not null, "Expected non-null streaming delegate.");
            return _completeStreamingFunc!(chatMessages, options, InnerClient, cancellationToken)
                .ToChatCompletionAsync(coalesceContent: true, cancellationToken);
        }
    }

    /// <inheritdoc/>
    public override IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(chatMessages);

        if (_sharedFunc is not null)
        {
            var updates = Channel.CreateBounded<StreamingChatCompletionUpdate>(1);

#pragma warning disable CA2016 // explicitly not forwarding the cancellation token, as we need to ensure the channel is always completed
            _ = Task.Run(async () =>
#pragma warning restore CA2016
            {
                Exception? error = null;
                try
                {
                    await _sharedFunc(chatMessages, options, async (chatMessages, options, cancellationToken) =>
                    {
                        await foreach (var update in InnerClient.CompleteStreamingAsync(chatMessages, options, cancellationToken).ConfigureAwait(false))
                        {
                            await updates.Writer.WriteAsync(update, cancellationToken).ConfigureAwait(false);
                        }
                    }, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    error = ex;
                    throw;
                }
                finally
                {
                    _ = updates.Writer.TryComplete(error);
                }
            });

            return updates.Reader.ReadAllAsync(cancellationToken);
        }
        else if (_completeStreamingFunc is not null)
        {
            return _completeStreamingFunc(chatMessages, options, InnerClient, cancellationToken);
        }
        else
        {
            Debug.Assert(_completeFunc is not null, "Expected non-null non-streaming delegate.");
            return CompleteStreamingAsyncViaCompleteAsync(_completeFunc!(chatMessages, options, InnerClient, cancellationToken));

            static async IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsyncViaCompleteAsync(Task<ChatCompletion> task)
            {
                ChatCompletion completion = await task.ConfigureAwait(false);
                foreach (var update in completion.ToStreamingChatCompletionUpdates())
                {
                    yield return update;
                }
            }
        }
    }

    /// <summary>Throws an exception if both of the specified delegates are null.</summary>
    /// <exception cref="ArgumentNullException">Both <paramref name="completeFunc"/> and <paramref name="completeStreamingFunc"/> are <see langword="null"/>.</exception>
    internal static void ThrowIfBothDelegatesNull(object? completeFunc, object? completeStreamingFunc)
    {
        if (completeFunc is null && completeStreamingFunc is null)
        {
            Throw.ArgumentNullException(nameof(completeFunc), $"At least one of the {nameof(completeFunc)} or {nameof(completeStreamingFunc)} delegates must be non-null.");
        }
    }

    // Design note:
    // The following delegate could juse use Func<...>, but it's defined as a custom delegate type
    // in order to provide better discoverability / documentation / usability around its complicated
    // signature with the nextAsync delegate parameter.

    /// <summary>
    /// Represents a method used to call <see cref="IChatClient.CompleteAsync"/> or <see cref="IChatClient.CompleteStreamingAsync"/>.
    /// </summary>
    /// <param name="chatMessages">The chat content to send.</param>
    /// <param name="options">The chat options to configure the request.</param>
    /// <param name="nextAsync">
    /// A delegate that provides the implementation for the inner client's <see cref="IChatClient.CompleteAsync"/> or
    /// <see cref="IChatClient.CompleteStreamingAsync"/>. It should be invoked to continue the pipeline. It accepts
    /// the chat messages, options, and cancellation token, which are typically the same instances as provided to this method
    /// but need not be.
    /// </param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A <see cref="Task"/> that represents the completion of the operation.</returns>
    public delegate Task CompleteSharedFunc(
        IList<ChatMessage> chatMessages,
        ChatOptions? options,
        Func<IList<ChatMessage>, ChatOptions?, CancellationToken, Task> nextAsync,
        CancellationToken cancellationToken);
}
