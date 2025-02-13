// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI.Evaluation.Quality.Utilities;

namespace Microsoft.Extensions.AI.Evaluation.Quality;

public partial class RelevanceTruthAndCompletenessEvaluator
{
    internal sealed class Rating
    {
        public static Rating Inconclusive { get; } = new Rating(relevance: -1, truth: -1, completeness: -1);

        public int Relevance { get; }
        public string? RelevanceReasoning { get; }
        public string[] RelevanceReasons { get; } = [];

        public int Truth { get; }
        public string? TruthReasoning { get; }
        public string[] TruthReasons { get; } = [];

        public int Completeness { get; }
        public string? CompletenessReasoning { get; }
        public string[] CompletenessReasons { get; } = [];

        public string? Error { get; }

        private const int MinValue = 1;
        private const int MaxValue = 5;

#pragma warning disable S1067 // Expressions should not be too complex.
        public bool IsInconclusive =>
            Error is not null ||
            Relevance < MinValue || Relevance > MaxValue ||
            Truth < MinValue || Truth > MaxValue ||
            Completeness < MinValue || Completeness > MaxValue;
#pragma warning restore S1067

        public Rating(int relevance, int truth, int completeness, string? error = null)
        {
            (Relevance, Truth, Completeness, Error) = (relevance, truth, completeness, error);
        }

        [JsonConstructor]
#pragma warning disable S107 // Methods should not have too many parameters.
        public Rating(
            int relevance, string? relevanceReasoning, string[] relevanceReasons,
            int truth, string? truthReasoning, string[] truthReasons,
            int completeness, string? completenessReasoning, string[] completenessReasons,
            string? error = null)
#pragma warning restore S107
        {
            (Relevance, RelevanceReasoning, RelevanceReasons,
            Truth, TruthReasoning, TruthReasons,
            Completeness, CompletenessReasoning, CompletenessReasons,
            Error) =
                (relevance, relevanceReasoning, relevanceReasons ?? [],
                truth, truthReasoning, truthReasons ?? [],
                completeness, completenessReasoning, completenessReasons ?? [],
                error);
        }

        public static Rating FromJson(string jsonResponse)
        {
            ReadOnlySpan<char> trimmed = JsonOutputFixer.TrimMarkdownDelimiters(jsonResponse);
            return JsonSerializer.Deserialize(trimmed, SerializerContext.Default.Rating)!;
        }
    }
}
