// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI.Evaluation.Safety;

internal partial class ContentSafetyChatClient
{
    private sealed class NoOpChatClient : IChatClient
    {
        public static NoOpChatClient Instance { get; } = new NoOpChatClient();

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null)
            => null;

        public void Dispose()
        {
            // Do nothing.
        }
    }
}
