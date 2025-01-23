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
        public readonly int Relevance;
        public readonly string? RelevanceReasoning;
        public readonly string[] RelevanceReasons = [];

        public readonly int Truth;
        public readonly string? TruthReasoning;
        public readonly string[] TruthReasons = [];

        public readonly int Completeness;
        public readonly string? CompletenessReasoning;
        public readonly string[] CompletenessReasons = [];

        public readonly string? Error;

        internal static readonly Rating Inconclusive = new Rating(relevance: -1, truth: -1, completeness: -1);

        private const int MinValue = 1;
        private const int MaxValue = 5;

#pragma warning disable S1067 // Expressions should not be too complex.
        internal bool IsInconclusive =>
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

        internal static Rating FromJson(string jsonResponse)
        {
            ReadOnlySpan<char> trimmed = JsonOutputFixer.TrimMarkdownDelimiters(jsonResponse);
            return JsonSerializer.Deserialize(trimmed, SerializerContext.Default.Rating)!;
        }
    }
}
