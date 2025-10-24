// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Shared.Diagnostics;

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
    private readonly TextContent _request;

    /// <summary>
    /// Initializes a new instance of the <see cref="SentimentEnricher"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client used for sentiment analysis.</param>
    /// <param name="chatOptions">Options for the chat client.</param>
    /// <param name="confidenceThreshold">The confidence threshold for sentiment determination.</param>
    public SentimentEnricher(IChatClient chatClient, ChatOptions? chatOptions = null, double? confidenceThreshold = 0.7)
    {
        _chatClient = Throw.IfNull(chatClient);
        _chatOptions = chatOptions;

        double threshold = confidenceThreshold.HasValue ? Throw.IfOutOfRange(confidenceThreshold.Value, 0.0, 1.0) : 0.7;
        _request = new("You are a sentiment analysis expert. Analyze the sentiment of the given text and return Positive/Negative/Neutral or" +
            $" Unknown when confidence score is below {threshold}. Return just the value of the sentiment.");
    }

    /// <summary>
    /// Gets the metadata key used to store the sentiment.
    /// </summary>
    public static string MetadataKey => "sentiment";

    /// <inheritdoc/>
    public override async IAsyncEnumerable<IngestionChunk<string>> ProcessAsync(IAsyncEnumerable<IngestionChunk<string>> chunks,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(chunks);

        await foreach (var chunk in chunks.WithCancellation(cancellationToken))
        {
            var response = await _chatClient.GetResponseAsync(
            [
                new(ChatRole.User,
                [
                    _request,
                    new TextContent(chunk.Content),
                ])
            ], _chatOptions, cancellationToken: cancellationToken).ConfigureAwait(false);

            chunk.Metadata[MetadataKey] = response.Text;

            yield return chunk;
        }
    }
}
