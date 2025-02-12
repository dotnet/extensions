// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI;

public sealed class TestChatClient : IChatClient
{
    public TestChatClient()
    {
        GetServiceCallback = DefaultGetServiceCallback;
    }

    public IServiceProvider? Services { get; set; }

    public Func<IList<ChatMessage>, ChatOptions?, CancellationToken, Task<ChatResponse>>? GetResponseAsyncCallback { get; set; }

    public Func<IList<ChatMessage>, ChatOptions?, CancellationToken, IAsyncEnumerable<ChatResponseUpdate>>? GetStreamingResponseAsyncCallback { get; set; }

    public Func<Type, object?, object?> GetServiceCallback { get; set; }

    private object? DefaultGetServiceCallback(Type serviceType, object? serviceKey) =>
        serviceType is not null && serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public Task<ChatResponse> GetResponseAsync(IList<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => GetResponseAsyncCallback!.Invoke(chatMessages, options, cancellationToken);

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IList<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => GetStreamingResponseAsyncCallback!.Invoke(chatMessages, options, cancellationToken);

    public object? GetService(Type serviceType, object? serviceKey = null)
        => GetServiceCallback(serviceType, serviceKey);

    void IDisposable.Dispose()
    {
        // No resources need disposing.
    }
}
