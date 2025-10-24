// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DataIngestion;

/// <summary>
/// Enriches chunks with summary text using an AI chat model.
/// </summary>
/// <remarks>
/// It adds "summary" text metadata to each chunk.
/// </remarks>
public sealed class SummaryEnricher : IngestionChunkProcessor<string>
{
    private readonly IChatClient _chatClient;
    private readonly ChatOptions? _chatOptions;
    private readonly int _maxWordCount;

    public SummaryEnricher(IChatClient chatClient, ChatOptions? chatOptions = null, int? maxWordCount = null)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        _chatOptions = chatOptions;

        if (maxWordCount.HasValue && maxWordCount.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxWordCount), "Max word count must be greater than zero.");
        }

        _maxWordCount = maxWordCount ?? 100;
    }

    public static string MetadataKey => "summary";

    public override async IAsyncEnumerable<IngestionChunk<string>> ProcessAsync(IAsyncEnumerable<IngestionChunk<string>> chunks,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (chunks is null)
        {
            throw new ArgumentNullException(nameof(chunks));
        }

        await foreach (var chunk in chunks.WithCancellation(cancellationToken))
        {
            var response = await _chatClient.GetResponseAsync(
            [
                new(ChatRole.User,
                [
                    new TextContent($"Write a summary text for this text with less than {_maxWordCount} words. Return just the summary."),
                    new TextContent(chunk.Content),
                ])
            ], _chatOptions, cancellationToken: cancellationToken);

            chunk.Metadata[MetadataKey] = response.Text;

            yield return chunk;
        }
    }
}
