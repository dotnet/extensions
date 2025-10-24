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
/// Enriches chunks with sentiment analysis using an AI chat model.
/// </summary>
/// <remarks>
/// It adds "sentiment" metadata to each chunk. It can be Positive, Negative, Neutral or Unknown when confidence score is below the threshold.
/// </remarks>
public sealed class SentimentEnricher : IngestionChunkProcessor<string>
{
    private readonly IChatClient _chatClient;
    private readonly ChatOptions? _chatOptions;
    private readonly double _confidenceThreshold;

    public SentimentEnricher(IChatClient chatClient, ChatOptions? chatOptions = null, double? confidenceThreshold = 0.7)
    {
        if (confidenceThreshold.HasValue && (confidenceThreshold < 0.0 || confidenceThreshold > 1.0))
        {
            throw new ArgumentOutOfRangeException(nameof(confidenceThreshold), "The confidence threshold must be between 0.0 and 1.0.");
        }

        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        _chatOptions = chatOptions;
        _confidenceThreshold = confidenceThreshold ?? 0.7;
    }

    public static string MetadataKey => "sentiment";

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
                    new TextContent($"You are a sentiment analysis expert. Analyze the sentiment of the given text and return Positive/Negative/Neutral or Unknown when confidence score is below {_confidenceThreshold}. Return just the value of the sentiment."),
                    new TextContent(chunk.Content),
                ])
            ], _chatOptions, cancellationToken: cancellationToken);

            chunk.Metadata[MetadataKey] = response.Text;

            yield return chunk;
        }
    }
}
