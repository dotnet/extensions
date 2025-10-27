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
    private readonly IChatClient _chatClient;
    private readonly ChatOptions _chatOptions;
    private readonly FrozenSet<string>? _predefinedKeywords;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeywordEnricher"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client used for keyword extraction.</param>
    /// <param name="predefinedKeywords">The set of predefined keywords for extraction.</param>
    /// <param name="chatOptions">Options for the chat client.</param>
    /// <param name="maxKeywords">The maximum number of keywords to extract. When not provided, it defaults to 5.</param>
    /// <param name="confidenceThreshold">The confidence threshold for keyword inclusion. When not provided, it defaults to 0.7.</param>
    /// <remarks>
    /// If no predefined keywords are provided, the model will extract keywords based on the content alone.
    /// Such results may vary more significantly between different AI models.
    /// </remarks>
    public KeywordEnricher(IChatClient chatClient, ReadOnlySpan<string> predefinedKeywords,
        ChatOptions? chatOptions = null, int? maxKeywords = null, double? confidenceThreshold = null)
    {
        double threshold = confidenceThreshold.HasValue
            ? Throw.IfOutOfRange(confidenceThreshold.Value, 0.0, 1.0, nameof(confidenceThreshold))
            : 0.7;
        int keywordsCount = maxKeywords.HasValue
            ? Throw.IfLessThanOrEqual(maxKeywords.Value, 0, nameof(maxKeywords))
            : DefaultMaxKeywords;

        _chatClient = Throw.IfNull(chatClient);
        _predefinedKeywords = CreatePredfinedKeywords(predefinedKeywords);
        _chatOptions = CreateChatOptions(keywordsCount, predefinedKeywords, threshold, chatOptions);
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

        await foreach (IngestionChunk<string> chunk in chunks.WithCancellation(cancellationToken))
        {
            // Structured response is not used here because it's not part of Microsoft.Extensions.AI.Abstractions.
            var response = await _chatClient.GetResponseAsync(
            [
                new(ChatRole.User, chunk.Content)
            ], _chatOptions, cancellationToken: cancellationToken).ConfigureAwait(false);

#pragma warning disable EA0009 // Use 'System.MemoryExtensions.Split' for improved performance
            string[] keywords = response.Text.Split(';');
            if (_predefinedKeywords is not null)
            {
                foreach (var keyword in keywords)
                {
                    if (!_predefinedKeywords.Contains(keyword))
                    {
                        throw new InvalidOperationException($"The extracted keyword '{keyword}' is not in the predefined keywords list.");
                    }
                }
            }

            chunk.Metadata[MetadataKey] = keywords;

            yield return chunk;
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

    private static ChatOptions CreateChatOptions(int maxKeywords, ReadOnlySpan<string> predefinedKeywords, double confidenceThreshold, ChatOptions? userProvided)
    {
        StringBuilder sb = new($"You are a keyword extraction expert. Analyze the given text and extract up to {maxKeywords} most relevant keywords. ");

        if (predefinedKeywords.Length > 0)
        {
#pragma warning disable IDE0058 // Expression value is never used
            sb.Append("Focus on extracting keywords from the following predefined list: ");
            for (int i = 0; i < predefinedKeywords.Length; i++)
            {
                sb.Append(predefinedKeywords[i]);
                if (i < predefinedKeywords.Length - 1)
                {
                    sb.Append(", ");
                }
            }

            sb.Append(". ");
        }

        sb.Append("Exclude keywords with confidence score below ").Append(confidenceThreshold).Append('.');
        sb.Append(" Return just the keywords separated with ';'.");
#pragma warning restore IDE0058 // Expression value is never used

        ChatOptions result = userProvided?.Clone() ?? new();
        result.Instructions = sb.ToString();
        return result;
    }
}
