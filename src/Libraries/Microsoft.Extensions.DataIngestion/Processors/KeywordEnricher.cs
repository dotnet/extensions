// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
    private readonly IChatClient _chatClient;
    private readonly ChatOptions? _chatOptions;
    private readonly TextContent _request;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeywordEnricher"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client used for keyword extraction.</param>
    /// <param name="predefinedKeywords">The set of predefined keywords for extraction.</param>
    /// <param name="chatOptions">Options for the chat client.</param>
    /// <param name="maxKeywords">The maximum number of keywords to extract.</param>
    /// <param name="confidenceThreshold">The confidence threshold for keyword inclusion.</param>
    /// <remarks>
    /// If no predefined keywords are provided, the model will extract keywords based on the content alone.
    /// Such results may vary more significantly between different AI models.
    /// </remarks>
    public KeywordEnricher(IChatClient chatClient, ReadOnlySpan<string> predefinedKeywords,
        ChatOptions? chatOptions = null, int? maxKeywords = null, double? confidenceThreshold = null)
    {
        double threshold = confidenceThreshold.HasValue
            ? Throw.IfOutOfRange(confidenceThreshold.Value, 0.0, 1.0)
            : 0.7;

        _chatClient = Throw.IfNull(chatClient);
        _chatOptions = chatOptions;
        _request = CreateLlmRequest(maxKeywords ?? DefaultMaxKeywords, predefinedKeywords, threshold);
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
            ChatResponse<string[]> response = await _chatClient.GetResponseAsync<string[]>(
            [
                new(ChatRole.User,
                [
                    _request,
                    new TextContent(chunk.Content),
                ])
            ], _chatOptions, cancellationToken: cancellationToken).ConfigureAwait(false);

            chunk.Metadata[MetadataKey] = response.Result;

            yield return chunk;
        }
    }

    private static TextContent CreateLlmRequest(int maxKeywords, ReadOnlySpan<string> predefinedKeywords, double confidenceThreshold)
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
#pragma warning restore IDE0058 // Expression value is never used

        return new(sb.ToString());
    }
}
