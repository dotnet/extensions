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

/// <summary>A delegating audio transcription client that wraps an inner client with implementations provided by delegates.</summary>
public sealed class AnonymousDelegatingAudioTranscriptionClient : DelegatingAudioTranscriptionClient
{
    /// <summary>The delegate to use as the implementation of <see cref="TranscribeAsync"/>.</summary>
    private readonly Func<IList<IAsyncEnumerable<DataContent>>, AudioTranscriptionOptions?, IAudioTranscriptionClient, CancellationToken, Task<AudioTranscriptionResponse>>? _transcribeFunc;

    /// <summary>The delegate to use as the implementation of <see cref="TranscribeStreamingAsync"/>.</summary>
    /// <remarks>
    /// When non-<see langword="null"/>, this delegate is used as the implementation of <see cref="TranscribeStreamingAsync"/> and
    /// will be invoked with the same arguments as the method itself, along with a reference to the inner client.
    /// When <see langword="null"/>, <see cref="TranscribeStreamingAsync"/> will delegate directly to the inner client.
    /// </remarks>
    private readonly Func<
        IList<IAsyncEnumerable<DataContent>>, AudioTranscriptionOptions?, IAudioTranscriptionClient, CancellationToken, IAsyncEnumerable<AudioTranscriptionResponseUpdate>>? _transcribeStreamingFunc;

    /// <summary>The delegate to use as the implementation of both <see cref="TranscribeAsync"/> and <see cref="TranscribeStreamingAsync"/>.</summary>
    private readonly TranscribeSharedFunc? _sharedFunc;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnonymousDelegatingAudioTranscriptionClient"/> class.
    /// </summary>
    /// <param name="innerClient">The inner client.</param>
    /// <param name="sharedFunc">
    /// A delegate that provides the implementation for both <see cref="TranscribeAsync"/> and <see cref="TranscribeStreamingAsync"/>.
    /// In addition to the arguments for the operation, it's provided with a delegate to the inner client that should be
    /// used to perform the operation on the inner client. It will handle both the non-streaming and streaming cases.
    /// </param>
    /// <remarks>
    /// This overload may be used when the anonymous implementation needs to provide pre- and/or post-processing, but doesn't
    /// need to interact with the results of the operation, which will come from the inner client.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="innerClient"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="sharedFunc"/> is <see langword="null"/>.</exception>
    public AnonymousDelegatingAudioTranscriptionClient(IAudioTranscriptionClient innerClient, TranscribeSharedFunc sharedFunc)
        : base(innerClient)
    {
        _ = Throw.IfNull(sharedFunc);

        _sharedFunc = sharedFunc;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AnonymousDelegatingAudioTranscriptionClient"/> class.
    /// </summary>
    /// <param name="innerClient">The inner client.</param>
    /// <param name="transcribeFunc">
    /// A delegate that provides the implementation for <see cref="TranscribeAsync"/>. When <see langword="null"/>,
    /// <paramref name="transcribeStreamingFunc"/> must be non-null, and the implementation of <see cref="TranscribeAsync"/>
    /// will use <paramref name="transcribeStreamingFunc"/> for the implementation.
    /// </param>
    /// <param name="transcribeStreamingFunc">
    /// A delegate that provides the implementation for <see cref="TranscribeStreamingAsync"/>. When <see langword="null"/>,
    /// <paramref name="transcribeFunc"/> must be non-null, and the implementation of <see cref="TranscribeStreamingAsync"/>
    /// will use <paramref name="transcribeFunc"/> for the implementation.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="innerClient"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException">Both <paramref name="transcribeFunc"/> and <paramref name="transcribeStreamingFunc"/> are <see langword="null"/>.</exception>
    public AnonymousDelegatingAudioTranscriptionClient(
        IAudioTranscriptionClient innerClient,
        Func<IList<IAsyncEnumerable<DataContent>>, AudioTranscriptionOptions?, IAudioTranscriptionClient, CancellationToken, Task<AudioTranscriptionResponse>>? transcribeFunc,
        Func<
            IList<IAsyncEnumerable<DataContent>>,
            AudioTranscriptionOptions?, IAudioTranscriptionClient, CancellationToken, IAsyncEnumerable<AudioTranscriptionResponseUpdate>>? transcribeStreamingFunc)
        : base(innerClient)
    {
        ThrowIfBothDelegatesNull(transcribeFunc, transcribeStreamingFunc);

        _transcribeFunc = transcribeFunc;
        _transcribeStreamingFunc = transcribeStreamingFunc;
    }

    /// <inheritdoc/>
    public override Task<AudioTranscriptionResponse> TranscribeAsync(
        IList<IAsyncEnumerable<DataContent>> audioContents, AudioTranscriptionOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(audioContents);

        if (_sharedFunc is not null)
        {
            return TranscribeViaSharedAsync(audioContents, options, cancellationToken);

            async Task<AudioTranscriptionResponse> TranscribeViaSharedAsync(
                IList<IAsyncEnumerable<DataContent>> audioContents, AudioTranscriptionOptions? options, CancellationToken cancellationToken)
            {
                AudioTranscriptionResponse? completion = null;
                await _sharedFunc(audioContents, options, async (audioContents, options, cancellationToken) =>
                {
                    completion = await InnerClient.TranscribeAsync(audioContents, options, cancellationToken).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);

                if (completion is null)
                {
                    throw new InvalidOperationException("The wrapper completed successfully without producing a AudioTranscriptionResponse.");
                }

                return completion;
            }
        }
        else if (_transcribeFunc is not null)
        {
            return _transcribeFunc(audioContents, options, InnerClient, cancellationToken);
        }
        else
        {
            Debug.Assert(_transcribeStreamingFunc is not null, "Expected non-null streaming delegate.");
            return _transcribeStreamingFunc!(audioContents, options, InnerClient, cancellationToken)
                .ToAudioTranscriptionCompletionAsync(coalesceContent: true, cancellationToken);
        }
    }

    /// <inheritdoc/>
    public override IAsyncEnumerable<AudioTranscriptionResponseUpdate> TranscribeStreamingAsync(
        IList<IAsyncEnumerable<DataContent>> audioContents, AudioTranscriptionOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(audioContents);

        if (_sharedFunc is not null)
        {
            var updates = Channel.CreateBounded<AudioTranscriptionResponseUpdate>(1);

#pragma warning disable CA2016 // explicitly not forwarding the cancellation token, as we need to ensure the channel is always completed
            _ = Task.Run(async () =>
#pragma warning restore CA2016
            {
                Exception? error = null;
                try
                {
                    await _sharedFunc(audioContents, options, async (audioContents, options, cancellationToken) =>
                    {
                        await foreach (var update in InnerClient.TranscribeStreamingAsync(audioContents, options, cancellationToken).ConfigureAwait(false))
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
        else if (_transcribeStreamingFunc is not null)
        {
            return _transcribeStreamingFunc(audioContents, options, InnerClient, cancellationToken);
        }
        else
        {
            Debug.Assert(_transcribeFunc is not null, "Expected non-null non-streaming delegate.");
            return TranscribeStreamingAsyncViaTranscribeAsync(_transcribeFunc!(audioContents, options, InnerClient, cancellationToken));

            static async IAsyncEnumerable<AudioTranscriptionResponseUpdate> TranscribeStreamingAsyncViaTranscribeAsync(Task<AudioTranscriptionResponse> task)
            {
                AudioTranscriptionResponse completion = await task.ConfigureAwait(false);
                foreach (var update in completion.ToStreamingAudioTranscriptionUpdates())
                {
                    yield return update;
                }
            }
        }
    }

    /// <summary>Throws an exception if both of the specified delegates are null.</summary>
    /// <exception cref="ArgumentNullException">Both <paramref name="transcribeFunc"/> and <paramref name="transcribeStreamingFunc"/> are <see langword="null"/>.</exception>
    internal static void ThrowIfBothDelegatesNull(object? transcribeFunc, object? transcribeStreamingFunc)
    {
        if (transcribeFunc is null && transcribeStreamingFunc is null)
        {
            Throw.ArgumentNullException(nameof(transcribeFunc), $"At least one of the {nameof(transcribeFunc)} or {nameof(transcribeStreamingFunc)} delegates must be non-null.");
        }
    }

    // Design note:
    // The following delegate could juse use Func<...>, but it's defined as a custom delegate type
    // in order to provide better discoverability / documentation / usability around its complicated
    // signature with the nextAsync delegate parameter.

    /// <summary>
    /// Represents a method used to call <see cref="IAudioTranscriptionClient.TranscribeAsync"/> or <see cref="IAudioTranscriptionClient.TranscribeStreamingAsync"/>.
    /// </summary>
    /// <param name="audioContents">The audio contents to send.</param>
    /// <param name="options">The audio transcription options to configure the request.</param>
    /// <param name="nextAsync">
    /// A delegate that provides the implementation for the inner client's <see cref="IAudioTranscriptionClient.TranscribeAsync"/> or
    /// <see cref="IAudioTranscriptionClient.TranscribeStreamingAsync"/>. It should be invoked to continue the pipeline. It accepts
    /// the audio contents, options, and cancellation token, which are typically the same instances as provided to this method
    /// but need not be.
    /// </param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A <see cref="Task"/> that represents the completion of the operation.</returns>
    public delegate Task TranscribeSharedFunc(
        IList<IAsyncEnumerable<DataContent>> audioContents,
        AudioTranscriptionOptions? options,
        Func<IList<IAsyncEnumerable<DataContent>>, AudioTranscriptionOptions?, CancellationToken, Task> nextAsync,
        CancellationToken cancellationToken);
}
