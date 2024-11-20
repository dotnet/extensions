// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI;

public sealed class TestChatClient : IChatClient
{
    public IServiceProvider? Services { get; set; }

    public ChatClientMetadata Metadata { get; set; } = new();

    public Func<IList<ChatMessage>, ChatOptions?, CancellationToken, Task<ChatCompletion>>? CompleteAsyncCallback { get; set; }

    public Func<IList<ChatMessage>, ChatOptions?, CancellationToken, IAsyncEnumerable<StreamingChatCompletionUpdate>>? CompleteStreamingAsyncCallback { get; set; }

    public Func<Type, object?, object?> GetServiceCallback { get; set; } = (_, _) => null;

    public Task<ChatCompletion> CompleteAsync(IList<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => CompleteAsyncCallback!.Invoke(chatMessages, options, cancellationToken);

    public IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(IList<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => CompleteStreamingAsyncCallback!.Invoke(chatMessages, options, cancellationToken);

    public object? GetService(Type serviceType, object? serviceKey = null)
        => GetServiceCallback(serviceType, serviceKey);

    void IDisposable.Dispose()
    {
        // No resources need disposing.
    }
}
