// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
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
    private readonly EnricherOptions _options;
    private readonly ChatMessage _systemPrompt;
    private readonly ILogger? _logger;

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
        Validate(predefinedKeywords);

        double threshold = confidenceThreshold.HasValue
            ? Throw.IfOutOfRange(confidenceThreshold.Value, 0.0, 1.0, nameof(confidenceThreshold))
            : 0.7;
        int keywordsCount = maxKeywords.HasValue
            ? Throw.IfLessThanOrEqual(maxKeywords.Value, 0, nameof(maxKeywords))
            : DefaultMaxKeywords;
        _systemPrompt = CreateSystemPrompt(keywordsCount, predefinedKeywords, threshold);
        _logger = _options.LoggerFactory?.CreateLogger<KeywordEnricher>();
    }

    /// <summary>
    /// Gets the metadata key used to store the keywords.
    /// </summary>
    public static string MetadataKey => "keywords";

    /// <inheritdoc/>
    public override IAsyncEnumerable<IngestionChunk<string>> ProcessAsync(IAsyncEnumerable<IngestionChunk<string>> chunks, CancellationToken cancellationToken = default)
        => Batching.ProcessAsync<string[]>(chunks, _options, MetadataKey, _systemPrompt, _logger, cancellationToken);

    private static void Validate(ReadOnlySpan<string> predefinedKeywords)
    {
        if (predefinedKeywords.Length == 0)
        {
            return;
        }

        HashSet<string> result = new(StringComparer.Ordinal);
        foreach (string keyword in predefinedKeywords)
        {
            if (!result.Add(keyword))
            {
                Throw.ArgumentException(nameof(predefinedKeywords), $"Duplicate keyword found: '{keyword}'");
            }
        }
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
