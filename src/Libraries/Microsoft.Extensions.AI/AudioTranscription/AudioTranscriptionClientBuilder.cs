// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>A builder for creating pipelines of <see cref="IAudioTranscriptionClient"/>.</summary>
public sealed class AudioTranscriptionClientBuilder
{
    private readonly Func<IServiceProvider, IAudioTranscriptionClient> _innerClientFactory;

    /// <summary>The registered client factory instances.</summary>
    private List<Func<IAudioTranscriptionClient, IServiceProvider, IAudioTranscriptionClient>>? _clientFactories;

    /// <summary>Initializes a new instance of the <see cref="AudioTranscriptionClientBuilder"/> class.</summary>
    /// <param name="innerClient">The inner <see cref="IAudioTranscriptionClient"/> that represents the underlying backend.</param>
    public AudioTranscriptionClientBuilder(IAudioTranscriptionClient innerClient)
    {
        _ = Throw.IfNull(innerClient);
        _innerClientFactory = _ => innerClient;
    }

    /// <summary>Initializes a new instance of the <see cref="AudioTranscriptionClientBuilder"/> class.</summary>
    /// <param name="innerClientFactory">A callback that produces the inner <see cref="IAudioTranscriptionClient"/> that represents the underlying backend.</param>
    public AudioTranscriptionClientBuilder(Func<IServiceProvider, IAudioTranscriptionClient> innerClientFactory)
    {
        _innerClientFactory = Throw.IfNull(innerClientFactory);
    }

    /// <summary>Builds an <see cref="IAudioTranscriptionClient"/> that represents the entire pipeline. Calls to this instance will pass through each of the pipeline stages in turn.</summary>
    /// <param name="services">
    /// The <see cref="IServiceProvider"/> that should provide services to the <see cref="IAudioTranscriptionClient"/> instances.
    /// If null, an empty <see cref="IServiceProvider"/> will be used.
    /// </param>
    /// <returns>An instance of <see cref="IAudioTranscriptionClient"/> that represents the entire pipeline.</returns>
    public IAudioTranscriptionClient Build(IServiceProvider? services = null)
    {
        services ??= EmptyServiceProvider.Instance;
        var audioClient = _innerClientFactory(services);

        // To match intuitive expectations, apply the factories in reverse order, so that the first factory added is the outermost.
        if (_clientFactories is not null)
        {
            for (var i = _clientFactories.Count - 1; i >= 0; i--)
            {
                audioClient = _clientFactories[i](audioClient, services) ??
                    throw new InvalidOperationException(
                        $"The {nameof(AudioTranscriptionClientBuilder)} entry at index {i} returned null. " +
                        $"Ensure that the callbacks passed to {nameof(Use)} return non-null {nameof(IAudioTranscriptionClient)} instances.");
            }
        }

        return audioClient;
    }

    /// <summary>Adds a factory for an intermediate audio transcription client to the audio transcription client pipeline.</summary>
    /// <param name="clientFactory">The client factory function.</param>
    /// <returns>The updated <see cref="AudioTranscriptionClientBuilder"/> instance.</returns>
    public AudioTranscriptionClientBuilder Use(Func<IAudioTranscriptionClient, IAudioTranscriptionClient> clientFactory)
    {
        _ = Throw.IfNull(clientFactory);

        return Use((innerClient, _) => clientFactory(innerClient));
    }

    /// <summary>Adds a factory for an intermediate audio transcription client to the audio transcription client pipeline.</summary>
    /// <param name="clientFactory">The client factory function.</param>
    /// <returns>The updated <see cref="AudioTranscriptionClientBuilder"/> instance.</returns>
    public AudioTranscriptionClientBuilder Use(Func<IAudioTranscriptionClient, IServiceProvider, IAudioTranscriptionClient> clientFactory)
    {
        _ = Throw.IfNull(clientFactory);

        (_clientFactories ??= []).Add(clientFactory);
        return this;
    }

    /// <summary>
    /// Adds to the audio transcription client pipeline an anonymous delegating audio transcription client based on a delegate that provides
    /// an implementation for both <see cref="IAudioTranscriptionClient.TranscribeAsync"/> and <see cref="IAudioTranscriptionClient.TranscribeStreamingAsync"/>.
    /// </summary>
    /// <param name="sharedFunc">
    /// A delegate that provides the implementation for both <see cref="IAudioTranscriptionClient.TranscribeAsync"/> and
    /// <see cref="IAudioTranscriptionClient.TranscribeStreamingAsync"/>. In addition to the arguments for the operation, it's
    /// provided with a delegate to the inner client that should be used to perform the operation on the inner client.
    /// It will handle both the non-streaming and streaming cases.
    /// </param>
    /// <returns>The updated <see cref="AudioTranscriptionClientBuilder"/> instance.</returns>
    /// <remarks>
    /// This overload may be used when the anonymous implementation needs to provide pre- and/or post-processing, but doesn't
    /// need to interact with the results of the operation, which will come from the inner client.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="sharedFunc"/> is <see langword="null"/>.</exception>
    public AudioTranscriptionClientBuilder Use(AnonymousDelegatingAudioTranscriptionClient.TranscribeSharedFunc sharedFunc)
    {
        _ = Throw.IfNull(sharedFunc);

        return Use((innerClient, _) => new AnonymousDelegatingAudioTranscriptionClient(innerClient, sharedFunc));
    }

    /// <summary>
    /// Adds to the audio transcription client pipeline an anonymous delegating audio transcription client based on a delegate that provides
    /// an implementation for both <see cref="IAudioTranscriptionClient.TranscribeAsync"/> and <see cref="IAudioTranscriptionClient.TranscribeStreamingAsync"/>.
    /// </summary>
    /// <param name="transcribeFunc">
    /// A delegate that provides the implementation for <see cref="IAudioTranscriptionClient.TranscribeAsync"/>. When <see langword="null"/>,
    /// <paramref name="transcribeStreamingFunc"/> must be non-null, and the implementation of <see cref="IAudioTranscriptionClient.TranscribeAsync"/>
    /// will use <paramref name="transcribeStreamingFunc"/> for the implementation.
    /// </param>
    /// <param name="transcribeStreamingFunc">
    /// A delegate that provides the implementation for <see cref="IAudioTranscriptionClient.TranscribeStreamingAsync"/>. When <see langword="null"/>,
    /// <paramref name="transcribeFunc"/> must be non-null, and the implementation of <see cref="IAudioTranscriptionClient.TranscribeStreamingAsync"/>
    /// will use <paramref name="transcribeFunc"/> for the implementation.
    /// </param>
    /// <returns>The updated <see cref="AudioTranscriptionClientBuilder"/> instance.</returns>
    /// <remarks>
    /// One or both delegates may be provided. If both are provided, they will be used for their respective methods:
    /// <paramref name="transcribeFunc"/> will provide the implementation of <see cref="IAudioTranscriptionClient.TranscribeAsync"/>, and
    /// <paramref name="transcribeStreamingFunc"/> will provide the implementation of <see cref="IAudioTranscriptionClient.TranscribeStreamingAsync"/>.
    /// If only one of the delegates is provided, it will be used for both methods. That means that if <paramref name="transcribeFunc"/>
    /// is supplied without <paramref name="transcribeStreamingFunc"/>, the implementation of <see cref="IAudioTranscriptionClient.TranscribeStreamingAsync"/>
    /// will employ limited streaming, as it will be operating on the batch output produced by <paramref name="transcribeFunc"/>. And if
    /// <paramref name="transcribeStreamingFunc"/> is supplied without <paramref name="transcribeFunc"/>, the implementation of
    /// <see cref="IAudioTranscriptionClient.TranscribeAsync"/> will be implemented by combining the updates from <paramref name="transcribeStreamingFunc"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Both <paramref name="transcribeFunc"/> and <paramref name="transcribeStreamingFunc"/> are <see langword="null"/>.</exception>
    public AudioTranscriptionClientBuilder Use(
        Func<IList<IAsyncEnumerable<DataContent>>, AudioTranscriptionOptions?, IAudioTranscriptionClient, CancellationToken, Task<AudioTranscriptionResponse>>? transcribeFunc,
        Func<IList<IAsyncEnumerable<DataContent>>, AudioTranscriptionOptions?, IAudioTranscriptionClient, CancellationToken,
            IAsyncEnumerable<AudioTranscriptionResponseUpdate>>? transcribeStreamingFunc)
    {
        AnonymousDelegatingAudioTranscriptionClient.ThrowIfBothDelegatesNull(transcribeFunc, transcribeStreamingFunc);

        return Use((innerClient, _) => new AnonymousDelegatingAudioTranscriptionClient(innerClient, transcribeFunc, transcribeStreamingFunc));
    }
}
