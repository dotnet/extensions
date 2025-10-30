// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.Evaluation.Utilities;
using Microsoft.Extensions.Caching.Distributed;

namespace Microsoft.Extensions.AI.Evaluation.Reporting;

internal sealed class ResponseCachingChatClient : DistributedCachingChatClient
{
    private readonly ChatDetails _chatDetails;
    private readonly ConcurrentDictionary<string, Stopwatch> _stopWatches;
    private readonly ChatClientMetadata? _metadata;

    internal ResponseCachingChatClient(
        IChatClient originalChatClient,
        IDistributedCache cache,
        IEnumerable<string> cachingKeys,
        ChatDetails chatDetails)
            : base(originalChatClient, cache)
    {
        CacheKeyAdditionalValues = [.. cachingKeys];

        _chatDetails = chatDetails;
        _stopWatches = new ConcurrentDictionary<string, Stopwatch>();
        _metadata = this.GetService<ChatClientMetadata>();
    }

    protected override async Task<ChatResponse?> ReadCacheAsync(string key, CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        ChatResponse? response = await base.ReadCacheAsync(key, cancellationToken).ConfigureAwait(false);

        if (response is null)
        {
            _ = _stopWatches.AddOrUpdate(key, addValue: stopwatch, updateValueFactory: (_, _) => stopwatch);
        }
        else
        {
            stopwatch.Stop();

            string? model = response.ModelId;
            string? modelProvider = ModelInfo.GetModelProvider(model, _metadata);

            _chatDetails.AddTurnDetails(
                new ChatTurnDetails(
                    latency: stopwatch.Elapsed,
                    model,
                    modelProvider,
                    usage: response.Usage,
                    cacheKey: key,
                    cacheHit: true));
        }

        return response;
    }

    protected override async Task<IReadOnlyList<ChatResponseUpdate>?> ReadCacheStreamingAsync(
        string key,
        CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        IReadOnlyList<ChatResponseUpdate>? updates =
            await base.ReadCacheStreamingAsync(key, cancellationToken).ConfigureAwait(false);

        if (updates is null)
        {
            _ = _stopWatches.AddOrUpdate(key, addValue: stopwatch, updateValueFactory: (_, _) => stopwatch);
        }
        else
        {
            stopwatch.Stop();

            ChatResponse response = updates.ToChatResponse();
            string? model = response.ModelId;
            string? modelProvider = ModelInfo.GetModelProvider(model, _metadata);

            _chatDetails.AddTurnDetails(
                new ChatTurnDetails(
                    latency: stopwatch.Elapsed,
                    model,
                    modelProvider,
                    usage: response.Usage,
                    cacheKey: key,
                    cacheHit: true));
        }

        return updates;
    }

    protected override async Task WriteCacheAsync(string key, ChatResponse value, CancellationToken cancellationToken)
    {
        await base.WriteCacheAsync(key, value, cancellationToken).ConfigureAwait(false);

        if (_stopWatches.TryRemove(key, out Stopwatch? stopwatch))
        {
            stopwatch.Stop();

            string? model = value.ModelId;
            string? modelProvider = ModelInfo.GetModelProvider(model, _metadata);

            _chatDetails.AddTurnDetails(
                new ChatTurnDetails(
                    latency: stopwatch.Elapsed,
                    model,
                    modelProvider,
                    usage: value.Usage,
                    cacheKey: key,
                    cacheHit: false));
        }
    }

    protected override async Task WriteCacheStreamingAsync(
        string key,
        IReadOnlyList<ChatResponseUpdate> value,
        CancellationToken cancellationToken)
    {
        await base.WriteCacheStreamingAsync(key, value, cancellationToken).ConfigureAwait(false);

        if (_stopWatches.TryRemove(key, out Stopwatch? stopwatch))
        {
            stopwatch.Stop();

            ChatResponse response = value.ToChatResponse();
            string? model = response.ModelId;
            string? modelProvider = ModelInfo.GetModelProvider(model, _metadata);

            _chatDetails.AddTurnDetails(
                new ChatTurnDetails(
                    latency: stopwatch.Elapsed,
                    model,
                    modelProvider,
                    usage: response.Usage,
                    cacheKey: key,
                    cacheHit: false));
        }
    }
}
