// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI;

public sealed class TestSpeechToTextClient : ISpeechToTextClient
{
    public TestSpeechToTextClient()
    {
        GetServiceCallback = DefaultGetServiceCallback;
    }

    public IServiceProvider? Services { get; set; }

    // Callbacks for asynchronous operations.
    public Func<Stream,
        SpeechToTextOptions?,
        CancellationToken,
        Task<SpeechToTextResponse>>?
        GetTextAsyncCallback
    { get; set; }

    public Func<Stream,
        SpeechToTextOptions?,
        CancellationToken,
        IAsyncEnumerable<SpeechToTextResponseUpdate>>?
        GetStreamingTextAsyncCallback
    { get; set; }

    public Func<Type, object?, object?> GetServiceCallback { get; set; }

    private object? DefaultGetServiceCallback(Type serviceType, object? serviceKey)
        => serviceType is not null && serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public Task<SpeechToTextResponse> GetTextAsync(
        Stream audioSpeechStream,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default)
        => GetTextAsyncCallback!.Invoke(audioSpeechStream, options, cancellationToken);

    public IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingTextAsync(
        Stream audioSpeechStream,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default)
        => GetStreamingTextAsyncCallback!.Invoke(audioSpeechStream, options, cancellationToken);

    public object? GetService(Type serviceType, object? serviceKey = null)
        => GetServiceCallback!.Invoke(serviceType, serviceKey);

    public void Dispose()
    {
        // Dispose of resources if any.
    }
}
