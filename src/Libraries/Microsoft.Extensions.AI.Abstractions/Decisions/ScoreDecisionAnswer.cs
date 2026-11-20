// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the answer to a <see cref="ScoreDecisionQuestion"/>.</summary>
/// <remarks>
/// Probabilities are reported by the provider and are not renormalized. Providers may round values, report
/// only some levels, report them in any order, or report none, so the distribution is not required to be complete
/// or to sum exactly to one. Whether the answer fits the question's levels is checked by the consumer that holds the question.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class ScoreDecisionAnswer : DecisionAnswer
{
    /// <summary>Initializes a new instance of the <see cref="ScoreDecisionAnswer"/> class.</summary>
    /// <param name="questionId">The exact question ID.</param>
    /// <param name="score">
    /// The provider-reported score as a zero-based position on the ordered level scale, or <see langword="null"/> when the provider
    /// reports only a distribution.
    /// </param>
    /// <param name="probabilities">The reported probability of each level, keyed by exact level ID. Can be empty.</param>
    /// <exception cref="ArgumentException"><paramref name="probabilities"/> contains a null entry or a duplicate ID.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="score"/> is negative or not finite.</exception>
    [JsonConstructor]
    public ScoreDecisionAnswer(string questionId, double? score, IReadOnlyList<DecisionProbability> probabilities)
        : base(questionId, DecisionKind.Score)
    {
        Probabilities = SnapshotDistribution(probabilities, nameof(probabilities));

        if (score is double value && (double.IsNaN(value) || double.IsInfinity(value) || value < 0d))
        {
            Throw.ArgumentOutOfRangeException(nameof(score), score, "The score must be a finite, non-negative position on the level scale.");
        }

        Score = score;
    }

    /// <summary>Gets the provider-reported score, or <see langword="null"/> when the provider reports only a distribution.</summary>
    public double? Score { get; }

    /// <summary>Gets the owned probability distribution over levels.</summary>
    public IReadOnlyList<DecisionProbability> Probabilities { get; }

    /// <summary>Computes the expected zero-based level position from <see cref="Probabilities"/>.</summary>
    /// <param name="question">The question this answer belongs to, which defines the level order.</param>
    /// <returns>
    /// The expectation over the reported levels, normalized by their total probability, or <see langword="null"/> when
    /// no reported level of <paramref name="question"/> has a positive probability.
    /// </returns>
    /// <remarks>
    /// Each probability is weighted by the position of its level ID in <see cref="ScoreDecisionQuestion.Levels"/>,
    /// so the result does not depend on the order in which the provider reported the distribution.
    /// Probabilities for IDs that are not levels of <paramref name="question"/> are ignored.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="question"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="question"/> does not have this answer's <see cref="DecisionAnswer.QuestionId"/>.</exception>
    public double? GetExpectedScore(ScoreDecisionQuestion question)
    {
        _ = Throw.IfNull(question);
        if (!string.Equals(question.Id, QuestionId, StringComparison.Ordinal))
        {
            Throw.ArgumentException(nameof(question), "The question ID does not match the answer's question ID.");
        }

        Dictionary<string, int> positions = new(question.Levels.Count, StringComparer.Ordinal);
        for (int i = 0; i < question.Levels.Count; i++)
        {
            positions[question.Levels[i].Id] = i;
        }

        double total = 0d;
        double weighted = 0d;
        foreach (DecisionProbability probability in Probabilities)
        {
            if (positions.TryGetValue(probability.Id, out int position))
            {
                total += probability.Value;
                weighted += probability.Value * position;
            }
        }

        return total > 0d ? weighted / total : null;
    }
}