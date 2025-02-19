// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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
    public Func<IList<
        IAsyncEnumerable<DataContent>>,
        SpeechToTextOptions,
        CancellationToken,
        Task<SpeechToTextResponse>>?
        GetResponseAsyncCallback
    { get; set; }

    public Func<IList<IAsyncEnumerable<DataContent>>,
        SpeechToTextOptions,
        CancellationToken,
        IAsyncEnumerable<SpeechToTextResponseUpdate>>?
        GetStreamingResponseAsyncCallback
    { get; set; }

    // A GetService callback similar to the one used in TestChatClient.
    public Func<Type, object?, object?> GetServiceCallback { get; set; }

    private object? DefaultGetServiceCallback(Type serviceType, object? serviceKey)
        => serviceType is not null && serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public Task<SpeechToTextResponse> GetResponseAsync(
        IList<IAsyncEnumerable<DataContent>> speechContents,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default)
        => GetResponseAsyncCallback!(speechContents, options ?? new SpeechToTextOptions(), cancellationToken);

    public IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingResponseAsync(
        IList<IAsyncEnumerable<DataContent>> speechContents,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default)
        => GetStreamingResponseAsyncCallback!(speechContents, options ?? new SpeechToTextOptions(), cancellationToken);

    public object? GetService(Type serviceType, object? serviceKey = null)
        => GetServiceCallback(serviceType, serviceKey);

    public void Dispose()
    {
        // Dispose of resources if any.
    }
}
