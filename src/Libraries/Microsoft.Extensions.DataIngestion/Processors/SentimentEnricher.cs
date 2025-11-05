// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
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
    private readonly ChatMessage _systemPrompt;
    private readonly ILogger? _logger;

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
        _logger = _options.LoggerFactory?.CreateLogger<SentimentEnricher>();
    }

    /// <summary>
    /// Gets the metadata key used to store the sentiment.
    /// </summary>
    public static string MetadataKey => "sentiment";

    /// <inheritdoc/>
    public override IAsyncEnumerable<IngestionChunk<string>> ProcessAsync(IAsyncEnumerable<IngestionChunk<string>> chunks, CancellationToken cancellationToken = default)
        => Batching.ProcessAsync<string>(chunks, _options, MetadataKey, _systemPrompt, _logger, cancellationToken);
}
