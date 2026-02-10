// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides usage details about a request/response.</summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class UsageDetails
{
    /// <summary>Gets or sets the number of tokens in the input.</summary>
    public long? InputTokenCount { get; set; }

    /// <summary>Gets or sets the number of tokens in the output.</summary>
    public long? OutputTokenCount { get; set; }

    /// <summary>Gets or sets the total number of tokens used to produce the response.</summary>
    public long? TotalTokenCount { get; set; }

    /// <summary>
    /// Gets or sets the number of input tokens that were read from a cache.
    /// </summary>
    /// <remarks>
    /// Cached input tokens should be counted as part of <see cref="InputTokenCount"/>.
    /// </remarks>
    public long? CachedInputTokenCount { get; set; }

    /// <summary>
    /// Gets or sets the number of "reasoning" / "thinking" tokens used internally
    /// by the model.
    /// </summary>
    /// <remarks>
    /// Reasoning tokens should be counted as part of <see cref="OutputTokenCount"/>.
    /// </remarks>
    public long? ReasoningTokenCount { get; set; }

    /// <summary>Gets or sets the number of audio input tokens used.</summary>
    /// <remarks>
    /// This property is used only when audio input tokens are involved.
    /// </remarks>
    [Experimental("MEAI001")]
    [JsonIgnore]
    public long? InputAudioTokenCount { get; set; }

    /// <summary>Gets or sets the number of text input tokens used.</summary>
    /// <remarks>
    /// This property is used only when having audio and text tokens. Otherwise InputTokenCount is sufficient.
    /// </remarks>
    [Experimental("MEAI001")]
    [JsonIgnore]
    public long? InputTextTokenCount { get; set; }

    /// <summary>Gets or sets the number of audio output tokens used.</summary>
    /// <remarks>
    /// This property is used only when audio output tokens are involved.
    /// </remarks>
    [Experimental("MEAI001")]
    [JsonIgnore]
    public long? OutputAudioTokenCount { get; set; }

    /// <summary>Gets or sets the number of text output tokens used.</summary>
    /// <remarks>
    /// This property is used only when having audio and text tokens. Otherwise OutputTokenCount is sufficient.
    /// </remarks>
    [Experimental("MEAI001")]
    [JsonIgnore]
    public long? OutputTextTokenCount { get; set; }

    /// <summary>Gets or sets a dictionary of additional usage counts.</summary>
    /// <remarks>
    /// All values set here are assumed to be summable. For example, when middleware makes multiple calls to an underlying
    /// service, it may sum the counts from multiple results to produce an overall <see cref="UsageDetails"/>.
    /// </remarks>
    public AdditionalPropertiesDictionary<long>? AdditionalCounts { get; set; }

    /// <summary>Adds usage data from another <see cref="UsageDetails"/> into this instance.</summary>
    /// <param name="usage">The source <see cref="UsageDetails"/> with which to augment this instance.</param>
    /// <exception cref="ArgumentNullException"><paramref name="usage"/> is <see langword="null"/>.</exception>
    public void Add(UsageDetails usage)
    {
        _ = Throw.IfNull(usage);

        InputTokenCount = NullableSum(InputTokenCount, usage.InputTokenCount);
        OutputTokenCount = NullableSum(OutputTokenCount, usage.OutputTokenCount);
        TotalTokenCount = NullableSum(TotalTokenCount, usage.TotalTokenCount);
        CachedInputTokenCount = NullableSum(CachedInputTokenCount, usage.CachedInputTokenCount);
        ReasoningTokenCount = NullableSum(ReasoningTokenCount, usage.ReasoningTokenCount);
        InputAudioTokenCount = NullableSum(InputAudioTokenCount, usage.InputAudioTokenCount);
        InputTextTokenCount = NullableSum(InputTextTokenCount, usage.InputTextTokenCount);

        if (usage.AdditionalCounts is { } countsToAdd)
        {
            if (AdditionalCounts is null)
            {
                AdditionalCounts = new(countsToAdd);
            }
            else
            {
                foreach (var kvp in countsToAdd)
                {
                    AdditionalCounts[kvp.Key] = AdditionalCounts.TryGetValue(kvp.Key, out var existingValue) ?
                        kvp.Value + existingValue :
                        kvp.Value;
                }
            }
        }
    }

    /// <summary>Gets a string representing this instance to display in the debugger.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal string DebuggerDisplay
    {
        get
        {
            List<string> parts = [];

            if (InputTokenCount is { } input)
            {
                parts.Add($"{nameof(InputTokenCount)} = {input}");
            }

            if (OutputTokenCount is { } output)
            {
                parts.Add($"{nameof(OutputTokenCount)} = {output}");
            }

            if (TotalTokenCount is { } total)
            {
                parts.Add($"{nameof(TotalTokenCount)} = {total}");
            }

            if (CachedInputTokenCount is { } cached)
            {
                parts.Add($"{nameof(CachedInputTokenCount)} = {cached}");
            }

            if (ReasoningTokenCount is { } reasoning)
            {
                parts.Add($"{nameof(ReasoningTokenCount)} = {reasoning}");
            }

            if (InputAudioTokenCount is { } inputAudio)
            {
                parts.Add($"{nameof(InputAudioTokenCount)} = {inputAudio}");
            }

            if (InputTextTokenCount is { } inputText)
            {
                parts.Add($"{nameof(InputTextTokenCount)} = {inputText}");
            }

            if (AdditionalCounts is { } additionalCounts)
            {
                foreach (var entry in additionalCounts)
                {
                    parts.Add($"{entry.Key} = {entry.Value}");
                }
            }

            return string.Join(", ", parts);
        }
    }

    private static long? NullableSum(long? a, long? b) => (a.HasValue || b.HasValue) ? (a ?? 0) + (b ?? 0) : null;
}
