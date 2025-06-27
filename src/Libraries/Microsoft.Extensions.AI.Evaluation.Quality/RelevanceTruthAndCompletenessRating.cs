// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI.Evaluation.Quality.JsonSerialization;
using Microsoft.Extensions.AI.Evaluation.Quality.Utilities;

namespace Microsoft.Extensions.AI.Evaluation.Quality;

internal sealed class RelevanceTruthAndCompletenessRating
{
    public static RelevanceTruthAndCompletenessRating Inconclusive { get; } =
        new RelevanceTruthAndCompletenessRating(
            relevance: 0,
            relevanceReasoning: string.Empty,
            relevanceReasons: [],
            truth: 0,
            truthReasoning: string.Empty,
            truthReasons: [],
            completeness: 0,
            completenessReasoning: string.Empty,
            completenessReasons: []);

    [JsonRequired]
    public int Relevance { get; set; }

    [JsonRequired]
    public string RelevanceReasoning { get; set; }

    [JsonRequired]
    public string[] RelevanceReasons { get; set; }

    [JsonRequired]
    public int Truth { get; set; }

    [JsonRequired]
    public string TruthReasoning { get; set; }

    [JsonRequired]
    public string[] TruthReasons { get; set; }

    [JsonRequired]
    public int Completeness { get; set; }

    [JsonRequired]
    public string CompletenessReasoning { get; set; }

    [JsonRequired]
    public string[] CompletenessReasons { get; set; }

    private const int MinValue = 1;
    private const int MaxValue = 5;

#pragma warning disable S1067 // Expressions should not be too complex.
    public bool IsInconclusive =>
        Relevance < MinValue || Relevance > MaxValue ||
        Truth < MinValue || Truth > MaxValue ||
        Completeness < MinValue || Completeness > MaxValue;
#pragma warning restore S1067

    [JsonConstructor]
#pragma warning disable S107 // Methods should not have too many parameters.
    public RelevanceTruthAndCompletenessRating(
        int relevance, string relevanceReasoning, string[] relevanceReasons,
        int truth, string truthReasoning, string[] truthReasons,
        int completeness, string completenessReasoning, string[] completenessReasons)
#pragma warning restore S107
    {
        (Relevance, RelevanceReasoning, RelevanceReasons,
        Truth, TruthReasoning, TruthReasons,
        Completeness, CompletenessReasoning, CompletenessReasons) =
            (relevance, relevanceReasoning, relevanceReasons ?? [],
            truth, truthReasoning, truthReasons ?? [],
            completeness, completenessReasoning, completenessReasons ?? []);
    }

    public static RelevanceTruthAndCompletenessRating FromJson(string jsonResponse)
    {
        ReadOnlySpan<char> trimmed = JsonOutputFixer.TrimMarkdownDelimiters(jsonResponse);
        return JsonSerializer.Deserialize(trimmed, SerializerContext.Default.RelevanceTruthAndCompletenessRating)!;
    }
}
