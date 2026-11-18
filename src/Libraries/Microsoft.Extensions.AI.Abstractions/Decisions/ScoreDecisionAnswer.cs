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
/// Probabilities are reported by the provider and are not renormalized. Providers may round values,
/// so the distribution is not required to sum exactly to one.
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
    /// <param name="probabilities">The probability of each level, keyed by exact level ID, in level order.</param>
    /// <exception cref="ArgumentException">The distribution has no positive probability.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="score"/> is not a finite position on the level scale.</exception>
    [JsonConstructor]
    public ScoreDecisionAnswer(string questionId, double? score, IReadOnlyList<DecisionProbability> probabilities)
        : base(questionId, DecisionKind.Score)
    {
        Probabilities = SnapshotDistribution(probabilities, nameof(probabilities));

        if (score is double value && (double.IsNaN(value) || value < 0d || value > Probabilities.Count - 1))
        {
            Throw.ArgumentOutOfRangeException(nameof(score), score, "The score must be a finite position on the level scale.");
        }

        double total = 0d;
        double weighted = 0d;
        for (int i = 0; i < Probabilities.Count; i++)
        {
            total += Probabilities[i].Value;
            weighted += Probabilities[i].Value * i;
        }

        if (total <= 0d)
        {
            Throw.ArgumentException(nameof(probabilities), "A score distribution requires at least one positive probability.");
        }

        Score = score;
        ExpectedScore = weighted / total;
    }

    /// <summary>Gets the provider-reported score, or <see langword="null"/> when the provider reports only a distribution.</summary>
    public double? Score { get; }

    /// <summary>Gets the expected zero-based level position computed from <see cref="Probabilities"/>.</summary>
    /// <remarks>The expectation is normalized by the total reported probability mass.</remarks>
    [JsonIgnore]
    public double ExpectedScore { get; }

    /// <summary>Gets the owned probability distribution over levels, in level order.</summary>
    public IReadOnlyList<DecisionProbability> Probabilities { get; }
}
