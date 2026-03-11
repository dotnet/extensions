// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI;

public sealed class TestTextToSpeechClient : ITextToSpeechClient
{
    public TestTextToSpeechClient()
    {
        GetServiceCallback = DefaultGetServiceCallback;
    }

    public IServiceProvider? Services { get; set; }

    // Callbacks for asynchronous operations.
    public Func<string,
        TextToSpeechOptions?,
        CancellationToken,
        Task<TextToSpeechResponse>>?
        GetAudioAsyncCallback
    { get; set; }

    public Func<string,
        TextToSpeechOptions?,
        CancellationToken,
        IAsyncEnumerable<TextToSpeechResponseUpdate>>?
        GetStreamingAudioAsyncCallback
    { get; set; }

    public Func<Type, object?, object?> GetServiceCallback { get; set; }

    private object? DefaultGetServiceCallback(Type serviceType, object? serviceKey)
        => serviceType is not null && serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public Task<TextToSpeechResponse> GetAudioAsync(
        string text,
        TextToSpeechOptions? options = null,
        CancellationToken cancellationToken = default)
        => GetAudioAsyncCallback!.Invoke(text, options, cancellationToken);

    public IAsyncEnumerable<TextToSpeechResponseUpdate> GetStreamingAudioAsync(
        string text,
        TextToSpeechOptions? options = null,
        CancellationToken cancellationToken = default)
        => GetStreamingAudioAsyncCallback!.Invoke(text, options, cancellationToken);

    public object? GetService(Type serviceType, object? serviceKey = null)
        => GetServiceCallback!.Invoke(serviceType, serviceKey);

    public void Dispose()
    {
        // Dispose of resources if any.
    }
}
