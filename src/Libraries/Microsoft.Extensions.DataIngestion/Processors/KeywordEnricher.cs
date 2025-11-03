// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DataIngestion;

/// <summary>
/// Enriches chunks with keyword extraction using an AI chat model.
/// </summary>
/// <remarks>
/// It adds "keywords" metadata to each chunk. It's an array of strings representing the extracted keywords.
/// </remarks>
public sealed class KeywordEnricher : IngestionChunkProcessor<string>
{
    private const int DefaultMaxKeywords = 5;
#if NET
    private static readonly System.Buffers.SearchValues<char> _illegalCharacters = System.Buffers.SearchValues.Create([';', ',']);
#else
    private static readonly char[] _illegalCharacters = [';', ','];
#endif
    private readonly EnricherOptions _options;
    private readonly FrozenSet<string>? _predefinedKeywords;
    private readonly ChatMessage _systemPrompt;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeywordEnricher"/> class.
    /// </summary>
    /// <param name="options">The options for generating keywords.</param>
    /// <param name="predefinedKeywords">The set of predefined keywords for extraction.</param>
    /// <param name="maxKeywords">The maximum number of keywords to extract. When not provided, it defaults to 5.</param>
    /// <param name="confidenceThreshold">The confidence threshold for keyword inclusion. When not provided, it defaults to 0.7.</param>
    /// <remarks>
    /// If no predefined keywords are provided, the model will extract keywords based on the content alone.
    /// Such results may vary more significantly between different AI models.
    /// </remarks>
    public KeywordEnricher(EnricherOptions options, ReadOnlySpan<string> predefinedKeywords,
        int? maxKeywords = null, double? confidenceThreshold = null)
    {
        _options = Throw.IfNull(options).Clone();
        _predefinedKeywords = CreatePredfinedKeywords(predefinedKeywords);

        double threshold = confidenceThreshold.HasValue
            ? Throw.IfOutOfRange(confidenceThreshold.Value, 0.0, 1.0, nameof(confidenceThreshold))
            : 0.7;
        int keywordsCount = maxKeywords.HasValue
            ? Throw.IfLessThanOrEqual(maxKeywords.Value, 0, nameof(maxKeywords))
            : DefaultMaxKeywords;
        _systemPrompt = CreateSystemPrompt(keywordsCount, predefinedKeywords, threshold);
    }

    /// <summary>
    /// Gets the metadata key used to store the keywords.
    /// </summary>
    public static string MetadataKey => "keywords";

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

            var response = await _options.ChatClient.GetResponseAsync<string[][]>(
            [
                _systemPrompt,
                new(ChatRole.User, contents)
            ], _options.ChatOptions, cancellationToken: cancellationToken).ConfigureAwait(false);

            for (int i = 0; i < response.Result.Length; i++)
            {
                if (_predefinedKeywords is not null)
                {
                    foreach (string keyword in response.Result[i])
                    {
                        if (!_predefinedKeywords.Contains(keyword))
                        {
                            throw new InvalidOperationException($"The extracted keyword '{keyword}' is not in the predefined keywords list.");
                        }
                    }
                }

                batch[i].Metadata[MetadataKey] = response.Result[i];
            }

            foreach (var chunk in batch)
            {
                yield return chunk;
            }
        }
    }

    private static FrozenSet<string>? CreatePredfinedKeywords(ReadOnlySpan<string> predefinedKeywords)
    {
        if (predefinedKeywords.Length == 0)
        {
            return null;
        }

        HashSet<string> result = new(StringComparer.Ordinal);
        foreach (string keyword in predefinedKeywords)
        {
#if NET
            if (keyword.AsSpan().ContainsAny(_illegalCharacters))
#else
            if (keyword.IndexOfAny(_illegalCharacters) >= 0)
#endif
            {
                Throw.ArgumentException(nameof(predefinedKeywords), $"Predefined keyword '{keyword}' contains an invalid character (';' or ',').");
            }

            if (!result.Add(keyword))
            {
                Throw.ArgumentException(nameof(predefinedKeywords), $"Duplicate keyword found: '{keyword}'");
            }
        }

        return result.ToFrozenSet(StringComparer.Ordinal);
    }

    private static ChatMessage CreateSystemPrompt(int maxKeywords, ReadOnlySpan<string> predefinedKeywords, double confidenceThreshold)
    {
        StringBuilder sb = new($"You are a keyword extraction expert. For each of the following texts, extract up to {maxKeywords} most relevant keywords. ");

        if (predefinedKeywords.Length > 0)
        {
#pragma warning disable IDE0058 // Expression value is never used
            sb.Append("Focus on extracting keywords from the following predefined list: ");
#if NET9_0_OR_GREATER
            sb.AppendJoin(", ", predefinedKeywords!);
#else
            for (int i = 0; i < predefinedKeywords.Length; i++)
            {
                sb.Append(predefinedKeywords[i]);
                if (i < predefinedKeywords.Length - 1)
                {
                    sb.Append(", ");
                }
            }
#endif

            sb.Append(". ");
        }

        sb.Append("Exclude keywords with confidence score below ").Append(confidenceThreshold).Append('.');
#pragma warning restore IDE0058 // Expression value is never used

        return new(ChatRole.System, sb.ToString());
    }
}
