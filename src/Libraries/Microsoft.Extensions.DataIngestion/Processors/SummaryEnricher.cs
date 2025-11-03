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
/// Enriches chunks with summary text using an AI chat model.
/// </summary>
/// <remarks>
/// It adds "summary" text metadata to each chunk.
/// </remarks>
public sealed class SummaryEnricher : IngestionChunkProcessor<string>
{
    private readonly EnricherOptions _options;
    private readonly ChatMessage _systemPrompt;

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
    }

    /// <summary>
    /// Gets the metadata key used to store the summary.
    /// </summary>
    public static string MetadataKey => "summary";

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

            if (response.Result.Length != contents.Count)
            {
                throw new InvalidOperationException($"The AI chat service returned {response.Result.Length} instead of {contents.Count} results.");
            }

            for (int i = 0; i < response.Result.Length; i++)
            {
                batch[i].Metadata[MetadataKey] = response.Result[i];
            }

            foreach (var chunk in batch)
            {
                yield return chunk;
            }
        }
    }
}
