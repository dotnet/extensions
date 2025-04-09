// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
#if !NET9_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks

namespace Microsoft.Extensions.AI;

/// <summary>Represents a delegating chat client that wraps an inner client with implementations provided by delegates.</summary>
internal sealed class AnonymousDelegatingChatClient : DelegatingChatClient
{
    /// <summary>The delegate to use as the implementation of <see cref="GetResponseAsync"/>.</summary>
    private readonly Func<IEnumerable<ChatMessage>, ChatOptions?, IChatClient, CancellationToken, Task<ChatResponse>>? _getResponseFunc;

    /// <summary>The delegate to use as the implementation of <see cref="GetStreamingResponseAsync"/>.</summary>
    /// <remarks>
    /// When non-<see langword="null"/>, this delegate is used as the implementation of <see cref="GetStreamingResponseAsync"/> and
    /// will be invoked with the same arguments as the method itself, along with a reference to the inner client.
    /// When <see langword="null"/>, <see cref="GetStreamingResponseAsync"/> will delegate directly to the inner client.
    /// </remarks>
    private readonly Func<IEnumerable<ChatMessage>, ChatOptions?, IChatClient, CancellationToken, IAsyncEnumerable<ChatResponseUpdate>>? _getStreamingResponseFunc;

    /// <summary>The delegate to use as the implementation of both <see cref="GetResponseAsync"/> and <see cref="GetStreamingResponseAsync"/>.</summary>
    private readonly Func<IEnumerable<ChatMessage>, ChatOptions?, Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken, Task>, CancellationToken, Task>? _sharedFunc;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnonymousDelegatingChatClient"/> class.
    /// </summary>
    /// <param name="innerClient">The inner client.</param>
    /// <param name="sharedFunc">
    /// A delegate that provides the implementation for both <see cref="GetResponseAsync"/> and <see cref="GetStreamingResponseAsync"/>.
    /// In addition to the arguments for the operation, it's provided with a delegate to the inner client that should be
    /// used to perform the operation on the inner client. It will handle both the non-streaming and streaming cases.
    /// </param>
    /// <remarks>
    /// This overload may be used when the anonymous implementation needs to provide pre-processing and/or post-processing, but doesn't
    /// need to interact with the results of the operation, which will come from the inner client.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="innerClient"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="sharedFunc"/> is <see langword="null"/>.</exception>
    public AnonymousDelegatingChatClient(
        IChatClient innerClient,
        Func<IEnumerable<ChatMessage>, ChatOptions?, Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken, Task>, CancellationToken, Task> sharedFunc)
        : base(innerClient)
    {
        _ = Throw.IfNull(sharedFunc);

        _sharedFunc = sharedFunc;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AnonymousDelegatingChatClient"/> class.
    /// </summary>
    /// <param name="innerClient">The inner client.</param>
    /// <param name="getResponseFunc">
    /// A delegate that provides the implementation for <see cref="GetResponseAsync"/>. When <see langword="null"/>,
    /// <paramref name="getStreamingResponseFunc"/> must be non-null, and the implementation of <see cref="GetResponseAsync"/>
    /// will use <paramref name="getStreamingResponseFunc"/> for the implementation.
    /// </param>
    /// <param name="getStreamingResponseFunc">
    /// A delegate that provides the implementation for <see cref="GetStreamingResponseAsync"/>. When <see langword="null"/>,
    /// <paramref name="getResponseFunc"/> must be non-null, and the implementation of <see cref="GetStreamingResponseAsync"/>
    /// will use <paramref name="getResponseFunc"/> for the implementation.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="innerClient"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException">Both <paramref name="getResponseFunc"/> and <paramref name="getStreamingResponseFunc"/> are <see langword="null"/>.</exception>
    public AnonymousDelegatingChatClient(
        IChatClient innerClient,
        Func<IEnumerable<ChatMessage>, ChatOptions?, IChatClient, CancellationToken, Task<ChatResponse>>? getResponseFunc,
        Func<IEnumerable<ChatMessage>, ChatOptions?, IChatClient, CancellationToken, IAsyncEnumerable<ChatResponseUpdate>>? getStreamingResponseFunc)
        : base(innerClient)
    {
        ThrowIfBothDelegatesNull(getResponseFunc, getStreamingResponseFunc);

        _getResponseFunc = getResponseFunc;
        _getStreamingResponseFunc = getStreamingResponseFunc;
    }

    /// <inheritdoc/>
    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);

        if (_sharedFunc is not null)
        {
            return GetResponseViaSharedAsync(messages, options, cancellationToken);

            async Task<ChatResponse> GetResponseViaSharedAsync(
                IEnumerable<ChatMessage> messages, ChatOptions? options, CancellationToken cancellationToken)
            {
                ChatResponse? response = null;
                await _sharedFunc(messages, options, async (messages, options, cancellationToken) =>
                {
                    response = await InnerClient.GetResponseAsync(messages, options, cancellationToken);
                }, cancellationToken);

                if (response is null)
                {
                    Throw.InvalidOperationException("The wrapper completed successfully without producing a ChatResponse.");
                }

                return response;
            }
        }
        else if (_getResponseFunc is not null)
        {
            return _getResponseFunc(messages, options, InnerClient, cancellationToken);
        }
        else
        {
            Debug.Assert(_getStreamingResponseFunc is not null, "Expected non-null streaming delegate.");
            return _getStreamingResponseFunc!(messages, options, InnerClient, cancellationToken)
                .ToChatResponseAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);

        if (_sharedFunc is not null)
        {
            var updates = Channel.CreateBounded<ChatResponseUpdate>(1);

            _ = ProcessAsync();
            async Task ProcessAsync()
            {
                Exception? error = null;
                try
                {
                    await _sharedFunc(messages, options, async (messages, options, cancellationToken) =>
                    {
                        await foreach (var update in InnerClient.GetStreamingResponseAsync(messages, options, cancellationToken))
                        {
                            await updates.Writer.WriteAsync(update, cancellationToken);
                        }
                    }, cancellationToken);
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
            }

#if NET9_0_OR_GREATER
            return updates.Reader.ReadAllAsync(cancellationToken);
#else
            return ReadAllAsync(updates, cancellationToken);
            static async IAsyncEnumerable<ChatResponseUpdate> ReadAllAsync(
                ChannelReader<ChatResponseUpdate> channel, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                while (await channel.WaitToReadAsync(cancellationToken))
                {
                    while (channel.TryRead(out var update))
                    {
                        yield return update;
                    }
                }
            }
#endif
        }
        else if (_getStreamingResponseFunc is not null)
        {
            return _getStreamingResponseFunc(messages, options, InnerClient, cancellationToken);
        }
        else
        {
            Debug.Assert(_getResponseFunc is not null, "Expected non-null non-streaming delegate.");
            return GetStreamingResponseAsyncViaGetResponseAsync(_getResponseFunc!(messages, options, InnerClient, cancellationToken));

            static async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsyncViaGetResponseAsync(Task<ChatResponse> task)
            {
                ChatResponse response = await task;
                foreach (var update in response.ToChatResponseUpdates())
                {
                    yield return update;
                }
            }
        }
    }

    /// <summary>Throws an exception if both of the specified delegates are <see langword="null"/>.</summary>
    /// <exception cref="ArgumentNullException">Both <paramref name="getResponseFunc"/> and <paramref name="getStreamingResponseFunc"/> are <see langword="null"/>.</exception>
    internal static void ThrowIfBothDelegatesNull(object? getResponseFunc, object? getStreamingResponseFunc)
    {
        if (getResponseFunc is null && getStreamingResponseFunc is null)
        {
            Throw.ArgumentNullException(nameof(getResponseFunc), $"At least one of the {nameof(getResponseFunc)} or {nameof(getStreamingResponseFunc)} delegates must be non-null.");
        }
    }
}
