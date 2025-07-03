﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI.Evaluation.Reporting;

internal sealed class SimpleChatClient : DelegatingChatClient
{
    private readonly ChatDetails _chatDetails;

    internal SimpleChatClient(IChatClient originalChatClient, ChatDetails chatDetails)
        : base(originalChatClient)
    {
        _chatDetails = chatDetails;
    }

    public async override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ChatResponse? response = null;
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            response = await base.GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            stopwatch.Stop();

            if (response is not null)
            {
                _chatDetails.AddTurnDetails(
                    new ChatTurnDetails(
                        latency: stopwatch.Elapsed,
                        model: response.ModelId,
                        usage: response.Usage));
            }
        }

        return response;
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        List<ChatResponseUpdate>? updates = null;
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            await foreach (ChatResponseUpdate update in
                base.GetStreamingResponseAsync(messages, options, cancellationToken).ConfigureAwait(false))
            {
                updates ??= [];
                updates.Add(update);

                yield return update;
            }
        }
        finally
        {
            stopwatch.Stop();

            if (updates is not null)
            {
                ChatResponse response = updates.ToChatResponse();
                _chatDetails.AddTurnDetails(
                    new ChatTurnDetails(
                        latency: stopwatch.Elapsed,
                        model: response.ModelId,
                        usage: response.Usage));
            }
        }
    }
}
