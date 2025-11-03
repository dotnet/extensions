// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
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
    private readonly EnricherOptions _options;
    private readonly FrozenSet<string> _validSentiments =
#if NET9_0_OR_GREATER
        FrozenSet.Create(StringComparer.Ordinal, "Positive", "Negative", "Neutral", "Unknown");
#else
        new string[] { "Positive", "Negative", "Neutral", "Unknown" }.ToFrozenSet(StringComparer.Ordinal);
#endif
    private readonly ChatMessage _systemPrompt;

    /// <summary>
    /// Initializes a new instance of the <see cref="SentimentEnricher"/> class.
    /// </summary>
    /// <param name="options">The options for sentiment analysis.</param>
    /// <param name="confidenceThreshold">The confidence threshold for sentiment determination. When not provided, it defaults to 0.7.</param>
    public SentimentEnricher(EnricherOptions options, double? confidenceThreshold = null)
    {
        _options = Throw.IfNull(options).Clone();

        double threshold = confidenceThreshold.HasValue ? Throw.IfOutOfRange(confidenceThreshold.Value, 0.0, 1.0, nameof(confidenceThreshold)) : 0.7;

        string prompt = $"""
        You are a sentiment analysis expert. For each of the following texts, analyze the sentiment and return Positive/Negative/Neutral or
        Unknown when confidence score is below {threshold}.
        """;
        _systemPrompt = new(ChatRole.System, prompt);
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

        await foreach (var batch in chunks.BufferAsync(_options.BatchSize).WithCancellation(cancellationToken))
        {
            List<AIContent> contents = new(batch.Count);
            foreach (var chunk in batch)
            {
                contents.Add(new TextContent(chunk.Content));
            }

            var response = await _options.ChatClient.GetResponseAsync<string[]>(
            [
                _systemPrompt,
                new(ChatRole.User, contents)
            ], _options.ChatOptions, cancellationToken: cancellationToken).ConfigureAwait(false);

            for (int i = 0; i < response.Result.Length; i++)
            {
                batch[i].Metadata[MetadataKey] = _validSentiments.Contains(response.Result[i])
                    ? response.Result[i]
                    : throw new InvalidOperationException($"Invalid sentiment response: '{response.Result[i]}'.");
            }

            foreach (var chunk in batch)
            {
                yield return chunk;
            }
        }
    }
}
