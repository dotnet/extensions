// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI;

public sealed class TestAudioTranscriptionClient : IAudioTranscriptionClient
{
    public TestAudioTranscriptionClient()
    {
        GetServiceCallback = DefaultGetServiceCallback;
    }

    public IServiceProvider? Services { get; set; }

    // Callbacks for asynchronous operations.
    public Func<IList<
        IAsyncEnumerable<DataContent>>,
        AudioTranscriptionOptions,
        CancellationToken,
        Task<AudioTranscriptionResponse>>?
        TranscribeAsyncCallback
    { get; set; }

    public Func<IList<IAsyncEnumerable<DataContent>>,
        AudioTranscriptionOptions,
        CancellationToken,
        IAsyncEnumerable<AudioTranscriptionResponseUpdate>>?
        TranscribeStreamingAsyncCallback
    { get; set; }

    // A GetService callback similar to the one used in TestChatClient.
    public Func<Type, object?, object?> GetServiceCallback { get; set; }

    private object? DefaultGetServiceCallback(Type serviceType, object? serviceKey)
        => serviceType is not null && serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public Task<AudioTranscriptionResponse> TranscribeAsync(
        IList<IAsyncEnumerable<DataContent>> audioContents,
        AudioTranscriptionOptions? options = null,
        CancellationToken cancellationToken = default)
        => TranscribeAsyncCallback!(audioContents, options ?? new AudioTranscriptionOptions(), cancellationToken);

    public IAsyncEnumerable<AudioTranscriptionResponseUpdate> TranscribeStreamingAsync(
        IList<IAsyncEnumerable<DataContent>> audioContents,
        AudioTranscriptionOptions? options = null,
        CancellationToken cancellationToken = default)
        => TranscribeStreamingAsyncCallback!(audioContents, options ?? new AudioTranscriptionOptions(), cancellationToken);

    public object? GetService(Type serviceType, object? serviceKey = null)
        => GetServiceCallback(serviceType, serviceKey);

    public void Dispose()
    {
        // Dispose of resources if any.
    }
}
