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

/// <summary>A delegating speech to text client that wraps an inner client with implementations provided by delegates.</summary>
public sealed class AnonymousDelegatingSpeechToTextClient : DelegatingSpeechToTextClient
{
    /// <summary>The delegate to use as the implementation of <see cref="GetResponseAsync"/>.</summary>
    private readonly Func<IList<IAsyncEnumerable<DataContent>>, SpeechToTextOptions?, ISpeechToTextClient, CancellationToken, Task<SpeechToTextResponse>>? _getResponseFunc;

    /// <summary>The delegate to use as the implementation of <see cref="GetStreamingResponseAsync"/>.</summary>
    /// <remarks>
    /// When non-<see langword="null"/>, this delegate is used as the implementation of <see cref="GetStreamingResponseAsync"/> and
    /// will be invoked with the same arguments as the method itself, along with a reference to the inner client.
    /// When <see langword="null"/>, <see cref="GetStreamingResponseAsync"/> will delegate directly to the inner client.
    /// </remarks>
    private readonly Func<
        IList<IAsyncEnumerable<DataContent>>, SpeechToTextOptions?, ISpeechToTextClient, CancellationToken, IAsyncEnumerable<SpeechToTextResponseUpdate>>? _getStreamingResponseFunc;

    /// <summary>The delegate to use as the implementation of both <see cref="GetResponseAsync"/> and <see cref="GetStreamingResponseAsync"/>.</summary>
    private readonly GetResponseSharedFunc? _sharedFunc;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnonymousDelegatingSpeechToTextClient"/> class.
    /// </summary>
    /// <param name="innerClient">The inner client.</param>
    /// <param name="sharedFunc">
    /// A delegate that provides the implementation for both <see cref="GetResponseAsync"/> and <see cref="GetStreamingResponseAsync"/>.
    /// In addition to the arguments for the operation, it's provided with a delegate to the inner client that should be
    /// used to perform the operation on the inner client. It will handle both the non-streaming and streaming cases.
    /// </param>
    /// <remarks>
    /// This overload may be used when the anonymous implementation needs to provide pre- and/or post-processing, but doesn't
    /// need to interact with the results of the operation, which will come from the inner client.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="innerClient"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="sharedFunc"/> is <see langword="null"/>.</exception>
    public AnonymousDelegatingSpeechToTextClient(ISpeechToTextClient innerClient, GetResponseSharedFunc sharedFunc)
        : base(innerClient)
    {
        _ = Throw.IfNull(sharedFunc);

        _sharedFunc = sharedFunc;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AnonymousDelegatingSpeechToTextClient"/> class.
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
    public AnonymousDelegatingSpeechToTextClient(
        ISpeechToTextClient innerClient,
        Func<IList<IAsyncEnumerable<DataContent>>, SpeechToTextOptions?, ISpeechToTextClient, CancellationToken, Task<SpeechToTextResponse>>? getResponseFunc,
        Func<
            IList<IAsyncEnumerable<DataContent>>,
            SpeechToTextOptions?, ISpeechToTextClient, CancellationToken, IAsyncEnumerable<SpeechToTextResponseUpdate>>? getStreamingResponseFunc)
        : base(innerClient)
    {
        ThrowIfBothDelegatesNull(getResponseFunc, getStreamingResponseFunc);

        _getResponseFunc = getResponseFunc;
        _getStreamingResponseFunc = getStreamingResponseFunc;
    }

    /// <inheritdoc/>
    public override Task<SpeechToTextResponse> GetResponseAsync(
        IList<IAsyncEnumerable<DataContent>> speechContents, SpeechToTextOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(speechContents);

        if (_sharedFunc is not null)
        {
            return RespondViaSharedAsync(speechContents, options, cancellationToken);

            async Task<SpeechToTextResponse> RespondViaSharedAsync(
                IList<IAsyncEnumerable<DataContent>> audioContents, SpeechToTextOptions? options, CancellationToken cancellationToken)
            {
                SpeechToTextResponse? completion = null;
                await _sharedFunc(audioContents, options, async (audioContents, options, cancellationToken) =>
                {
                    completion = await InnerClient.GetResponseAsync(audioContents, options, cancellationToken).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);

                if (completion is null)
                {
                    throw new InvalidOperationException("The wrapper completed successfully without producing a SpeechToTextResponse.");
                }

                return completion;
            }
        }
        else if (_getResponseFunc is not null)
        {
            return _getResponseFunc(speechContents, options, InnerClient, cancellationToken);
        }
        else
        {
            Debug.Assert(_getStreamingResponseFunc is not null, "Expected non-null streaming delegate.");
            return _getStreamingResponseFunc!(speechContents, options, InnerClient, cancellationToken)
                .ToSpeechToTextResponseAsync(coalesceContent: true, cancellationToken);
        }
    }

    /// <inheritdoc/>
    public override IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingResponseAsync(
        IList<IAsyncEnumerable<DataContent>> speechContents, SpeechToTextOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(speechContents);

        if (_sharedFunc is not null)
        {
            var updates = Channel.CreateBounded<SpeechToTextResponseUpdate>(1);

#pragma warning disable CA2016 // explicitly not forwarding the cancellation token, as we need to ensure the channel is always completed
            _ = Task.Run(async () =>
#pragma warning restore CA2016
            {
                Exception? error = null;
                try
                {
                    await _sharedFunc(speechContents, options, async (speechContents, options, cancellationToken) =>
                    {
                        await foreach (var update in InnerClient.GetStreamingResponseAsync(speechContents, options, cancellationToken).ConfigureAwait(false))
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
        else if (_getStreamingResponseFunc is not null)
        {
            return _getStreamingResponseFunc(speechContents, options, InnerClient, cancellationToken);
        }
        else
        {
            Debug.Assert(_getResponseFunc is not null, "Expected non-null non-streaming delegate.");
            return GetStreamingResponseAsyncViaGetResponseAsync(_getResponseFunc!(speechContents, options, InnerClient, cancellationToken));

            static async IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingResponseAsyncViaGetResponseAsync(Task<SpeechToTextResponse> task)
            {
                SpeechToTextResponse completion = await task.ConfigureAwait(false);
                foreach (var update in completion.ToSpeechToTextResponseUpdates())
                {
                    yield return update;
                }
            }
        }
    }

    /// <summary>Throws an exception if both of the specified delegates are null.</summary>
    /// <exception cref="ArgumentNullException">Both <paramref name="getResponseFunc"/> and <paramref name="getStreamingResponseFunc"/> are <see langword="null"/>.</exception>
    internal static void ThrowIfBothDelegatesNull(object? getResponseFunc, object? getStreamingResponseFunc)
    {
        if (getResponseFunc is null && getStreamingResponseFunc is null)
        {
            Throw.ArgumentNullException(nameof(getResponseFunc), $"At least one of the {nameof(getResponseFunc)} or {nameof(getStreamingResponseFunc)} delegates must be non-null.");
        }
    }

    // Design note:
    // The following delegate could juse use Func<...>, but it's defined as a custom delegate type
    // in order to provide better discoverability / documentation / usability around its complicated
    // signature with the nextAsync delegate parameter.

    /// <summary>
    /// Represents a method used to call <see cref="ISpeechToTextClient.GetResponseAsync"/> or <see cref="ISpeechToTextClient.GetStreamingResponseAsync"/>.
    /// </summary>
    /// <param name="speechContents">The audio contents to send.</param>
    /// <param name="options">The speech to text options to configure the request.</param>
    /// <param name="nextAsync">
    /// A delegate that provides the implementation for the inner client's <see cref="ISpeechToTextClient.GetResponseAsync"/> or
    /// <see cref="ISpeechToTextClient.GetStreamingResponseAsync"/>. It should be invoked to continue the pipeline. It accepts
    /// the audio contents, options, and cancellation token, which are typically the same instances as provided to this method
    /// but need not be.
    /// </param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A <see cref="Task"/> that represents the completion of the operation.</returns>
    public delegate Task GetResponseSharedFunc(
        IList<IAsyncEnumerable<DataContent>> speechContents,
        SpeechToTextOptions? options,
        Func<IList<IAsyncEnumerable<DataContent>>, SpeechToTextOptions?, CancellationToken, Task> nextAsync,
        CancellationToken cancellationToken);
}
