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
/// Enriches chunks with summary text using an AI chat model.
/// </summary>
/// <remarks>
/// It adds "summary" text metadata to each chunk.
/// </remarks>
public sealed class SummaryEnricher : IngestionChunkProcessor<string>
{
    private readonly EnricherOptions _options;
    private readonly ChatMessage _systemPrompt;
    private readonly ILogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SummaryEnricher"/> class.
    /// </summary>
    /// <param name="options">The options for summary generation.</param>
    /// <param name="maxWordCount">The maximum number of words for the summary. When not provided, it defaults to 100.</param>
    public SummaryEnricher(EnricherOptions options, int? maxWordCount = null)
    {
        _options = Throw.IfNull(options).Clone();

        int wordCount = maxWordCount.HasValue ? Throw.IfLessThanOrEqual(maxWordCount.Value, 0, nameof(maxWordCount)) : 100;
        _systemPrompt = new(ChatRole.System, $"For each of the following texts, write a summary text with no more than {wordCount} words.");
        _logger = _options.LoggerFactory?.CreateLogger<SummaryEnricher>();
    }

    /// <summary>
    /// Gets the metadata key used to store the summary.
    /// </summary>
    public static string MetadataKey => "summary";

    /// <inheritdoc/>
    public override IAsyncEnumerable<IngestionChunk<string>> ProcessAsync(IAsyncEnumerable<IngestionChunk<string>> chunks, CancellationToken cancellationToken = default)
        => Batching.ProcessAsync<string>(chunks, _options, MetadataKey, _systemPrompt, _logger, cancellationToken);
}
