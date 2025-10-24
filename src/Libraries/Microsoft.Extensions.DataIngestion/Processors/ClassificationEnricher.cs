// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DataIngestion;

/// <summary>
/// Enriches document chunks with a classification label based on their content.
/// </summary>
/// <remarks>This class uses a chat-based language model to analyze the content of document chunks and assign a
/// single, most relevant classification label. The classification is performed using a predefined set of classes, with
/// an optional fallback class for cases where no suitable classification can be determined.</remarks>
public sealed class ClassificationEnricher : IngestionChunkProcessor<string>
{
    private readonly IChatClient _chatClient;
    private readonly ChatOptions? _chatOptions;
    private readonly TextContent _request;

    public ClassificationEnricher(IChatClient chatClient, ReadOnlySpan<string> predefinedClasses,
        ChatOptions? chatOptions = null, string? fallbackClass = null)
    {
        if (predefinedClasses.Length == 0)
        {
            throw new ArgumentException("Predefined classes must be provided.", nameof(predefinedClasses));
        }

        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        _chatOptions = chatOptions;
        _request = CreateLlmRequest(predefinedClasses, string.IsNullOrEmpty(fallbackClass) ? "Unknown" : fallbackClass!);
    }

    public static string MetadataKey => "classification";

    public override async IAsyncEnumerable<IngestionChunk<string>> ProcessAsync(IAsyncEnumerable<IngestionChunk<string>> chunks,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (chunks is null)
        {
            throw new ArgumentNullException(nameof(chunks));
        }

        await foreach (IngestionChunk<string> chunk in chunks.WithCancellation(cancellationToken))
        {
            var response = await _chatClient.GetResponseAsync(
            [
                new(ChatRole.User,
                [
                    _request,
                    new TextContent(chunk.Content),
                ])
            ], _chatOptions, cancellationToken: cancellationToken);

            chunk.Metadata[MetadataKey] = response.Text;

            yield return chunk;
        }
    }

    private static TextContent CreateLlmRequest(ReadOnlySpan<string> predefinedClasses, string fallbackClass)
        => new($"You are a classification expert. Analyze the given text and assign single, most relevant class. " +
            $"Use only the following predefined classes: {Join(predefinedClasses)} and return {fallbackClass} when unable to classify.");

    private static string Join(ReadOnlySpan<string> predefinedClasses)
        => string.Join(", ", predefinedClasses!
#if !NET
                .ToArray()
#endif
        );
}
