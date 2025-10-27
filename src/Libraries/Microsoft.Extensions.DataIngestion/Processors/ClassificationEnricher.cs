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
/// Enriches document chunks with a classification label based on their content.
/// </summary>
/// <remarks>This class uses a chat-based language model to analyze the content of document chunks and assign a
/// single, most relevant classification label. The classification is performed using a predefined set of classes, with
/// an optional fallback class for cases where no suitable classification can be determined.</remarks>
public sealed class ClassificationEnricher : IngestionChunkProcessor<string>
{
    private readonly IChatClient _chatClient;
    private readonly ChatOptions _chatOptions;
    private readonly FrozenSet<string> _predefinedClasses;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassificationEnricher"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client used for classification.</param>
    /// <param name="predefinedClasses">The set of predefined classification classes.</param>
    /// <param name="chatOptions">Options for the chat client.</param>
    /// <param name="fallbackClass">The fallback class to use when no suitable classification is found. When not provided, it defaults to "Unknown".</param>
    public ClassificationEnricher(IChatClient chatClient, ReadOnlySpan<string> predefinedClasses,
        ChatOptions? chatOptions = null, string? fallbackClass = null)
    {
        _chatClient = Throw.IfNull(chatClient);
        if (string.IsNullOrWhiteSpace(fallbackClass))
        {
            fallbackClass = "Unknown";
        }

        _predefinedClasses = CreatePredefinedSet(predefinedClasses, fallbackClass!);
        _chatOptions = CreateChatOptions(predefinedClasses, chatOptions, fallbackClass!);
    }

    /// <summary>
    /// Gets the metadata key used to store the classification.
    /// </summary>
    public static string MetadataKey => "classification";

    /// <inheritdoc />
    public override async IAsyncEnumerable<IngestionChunk<string>> ProcessAsync(IAsyncEnumerable<IngestionChunk<string>> chunks,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(chunks);

        await foreach (IngestionChunk<string> chunk in chunks.WithCancellation(cancellationToken))
        {
            var response = await _chatClient.GetResponseAsync(
            [
                new(ChatRole.User, chunk.Content)
            ], _chatOptions, cancellationToken: cancellationToken).ConfigureAwait(false);

            chunk.Metadata[MetadataKey] = _predefinedClasses.Contains(response.Text)
                ? response.Text
                : throw new InvalidOperationException($"Classification returned an unexpected class: '{response.Text}'.");

            yield return chunk;
        }
    }

    private static FrozenSet<string> CreatePredefinedSet(ReadOnlySpan<string> predefinedClasses, string fallbackClass)
    {
        if (predefinedClasses.Length == 0)
        {
            Throw.ArgumentException(nameof(predefinedClasses), "Predefined classes must be provided.");
        }

        HashSet<string> predefinedClassesSet = new(StringComparer.Ordinal) { fallbackClass };
        foreach (string predefinedClass in predefinedClasses)
        {
#if NET
            if (predefinedClass.Contains(',', StringComparison.Ordinal))
#else
            if (predefinedClass.IndexOf(',') >= 0)
#endif
            {
                Throw.ArgumentException(nameof(predefinedClasses), $"Predefined class '{predefinedClass}' must not contain ',' character.");
            }

            if (!predefinedClassesSet.Add(predefinedClass))
            {
                if (predefinedClass.Equals(fallbackClass, StringComparison.Ordinal))
                {
                    Throw.ArgumentException(nameof(predefinedClasses), $"Fallback class '{fallbackClass}' must not be one of the predefined classes.");
                }

                Throw.ArgumentException(nameof(predefinedClasses), $"Duplicate class found: '{predefinedClass}'.");
            }
        }

        return predefinedClassesSet.ToFrozenSet();
    }

    private static ChatOptions CreateChatOptions(ReadOnlySpan<string> predefinedClasses, ChatOptions? userProvided, string fallbackClass)
    {
        StringBuilder sb = new("You are a classification expert. Analyze the given text and assign a single, most relevant class. ");

#pragma warning disable IDE0058 // Expression value is never used
        sb.Append("Use only the following predefined classes: ");
        for (int i = 0; i < predefinedClasses.Length; i++)
        {
            sb.Append(predefinedClasses[i]);
            if (i < predefinedClasses.Length - 1)
            {
                sb.Append(", ");
            }
        }

        sb.Append(" and return ").Append(fallbackClass).Append(" when unable to classify.");
#pragma warning restore IDE0058 // Expression value is never used

        ChatOptions result = userProvided?.Clone() ?? new();
        result.Instructions = sb.ToString();
        return result;
    }
}
