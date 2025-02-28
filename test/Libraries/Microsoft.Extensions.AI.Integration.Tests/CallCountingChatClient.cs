// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1402 // File may only contain a single type

namespace Microsoft.Extensions.AI;

internal sealed class CallCountingChatClient(IChatClient innerClient) : DelegatingChatClient(innerClient)
{
    private int _callCount;

    public int CallCount => _callCount;

    public override Task<ChatResponse> GetResponseAsync(
        IList<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _callCount);
        return base.GetResponseAsync(chatMessages, options, cancellationToken);
    }

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IList<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _callCount);
        return base.GetStreamingResponseAsync(chatMessages, options, cancellationToken);
    }
}

internal static class CallCountingChatClientBuilderExtensions
{
    public static ChatClientBuilder UseCallCounting(this ChatClientBuilder builder) =>
        builder.Use(innerClient => new CallCountingChatClient(innerClient));
}
