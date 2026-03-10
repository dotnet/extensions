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
/// Enriches document chunks with a classification label based on their content.
/// </summary>
/// <remarks>This class uses a chat-based language model to analyze the content of document chunks and assign a
/// single, most relevant classification label. The classification is performed using a predefined set of classes, with
/// an optional fallback class for cases where no suitable classification can be determined.</remarks>
public sealed class ClassificationEnricher : IngestionChunkProcessor<string>
{
    private readonly EnricherOptions _options;
    private readonly ChatMessage _systemPrompt;
    private readonly ILogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassificationEnricher"/> class.
    /// </summary>
    /// <param name="options">The options for the classification enricher.</param>
    /// <param name="predefinedClasses">The set of predefined classification classes.</param>
    /// <param name="fallbackClass">The fallback class to use when no suitable classification is found. When not provided, it defaults to "Unknown".</param>
    public ClassificationEnricher(EnricherOptions options, ReadOnlySpan<string> predefinedClasses,
        string? fallbackClass = null)
    {
        _options = Throw.IfNull(options).Clone();
        if (string.IsNullOrWhiteSpace(fallbackClass))
        {
            fallbackClass = "Unknown";
        }

        Validate(predefinedClasses, fallbackClass!);
        _systemPrompt = CreateSystemPrompt(predefinedClasses, fallbackClass!);
        _logger = _options.LoggerFactory?.CreateLogger<ClassificationEnricher>();
    }

    /// <summary>
    /// Gets the metadata key used to store the classification.
    /// </summary>
    public static string MetadataKey => "classification";

    /// <inheritdoc />
    public override IAsyncEnumerable<IngestionChunk<string>> ProcessAsync(IAsyncEnumerable<IngestionChunk<string>> chunks, CancellationToken cancellationToken = default)
        => Batching.ProcessAsync<string>(chunks, _options, MetadataKey, _systemPrompt, _logger, cancellationToken);

    private static void Validate(ReadOnlySpan<string> predefinedClasses, string fallbackClass)
    {
        if (predefinedClasses.Length == 0)
        {
            Throw.ArgumentException(nameof(predefinedClasses), "Predefined classes must be provided.");
        }

        HashSet<string> predefinedClassesSet = new(StringComparer.Ordinal) { fallbackClass };
        foreach (string predefinedClass in predefinedClasses)
        {
            if (!predefinedClassesSet.Add(predefinedClass))
            {
                if (predefinedClass.Equals(fallbackClass, StringComparison.Ordinal))
                {
                    Throw.ArgumentException(nameof(predefinedClasses), $"Fallback class '{fallbackClass}' must not be one of the predefined classes.");
                }

                Throw.ArgumentException(nameof(predefinedClasses), $"Duplicate class found: '{predefinedClass}'.");
            }
        }
    }

    private static ChatMessage CreateSystemPrompt(ReadOnlySpan<string> predefinedClasses, string fallbackClass)
    {
        StringBuilder sb = new("You are a classification expert. For each of the following texts, assign a single, most relevant class. Use only the following predefined classes: ");

#if NET9_0_OR_GREATER
        sb.AppendJoin(", ", predefinedClasses!);
#else
#pragma warning disable IDE0058 // Expression value is never used
        for (int i = 0; i < predefinedClasses.Length; i++)
        {
            sb.Append(predefinedClasses[i]);
            if (i < predefinedClasses.Length - 1)
            {
                sb.Append(", ");
            }
        }
#endif
        sb.Append(" and return ").Append(fallbackClass).Append(" when unable to classify.");
#pragma warning restore IDE0058 // Expression value is never used

        return new(ChatRole.System, sb.ToString());
    }
}
