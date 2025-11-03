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
    private readonly IChatClient _chatClient;
    private readonly ChatOptions? _chatOptions;
    private readonly ChatMessage _systemPrompt;

    /// <summary>
    /// Initializes a new instance of the <see cref="SummaryEnricher"/> class.
    /// </summary>
    /// <param name="options">The options for summary generation.</param>
    /// <param name="maxWordCount">The maximum number of words for the summary. When not provided, it defaults to 100.</param>
    public SummaryEnricher(EnricherOptions options, int? maxWordCount = null)
    {
        _chatClient = Throw.IfNull(options).ChatClient;
        _chatOptions = options.ChatOptions;

        int wordCount = maxWordCount.HasValue ? Throw.IfLessThanOrEqual(maxWordCount.Value, 0, nameof(maxWordCount)) : 100;
        _systemPrompt = new(ChatRole.System, $"Write a summary text for this text with no more than {wordCount} words. Return just the summary.");
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

        await foreach (var chunk in chunks.WithCancellation(cancellationToken))
        {
            var response = await _chatClient.GetResponseAsync(
            [
                _systemPrompt,
                new(ChatRole.User, chunk.Content)
            ], _chatOptions, cancellationToken: cancellationToken).ConfigureAwait(false);

            chunk.Metadata[MetadataKey] = response.Text;

            yield return chunk;
        }
    }
}
