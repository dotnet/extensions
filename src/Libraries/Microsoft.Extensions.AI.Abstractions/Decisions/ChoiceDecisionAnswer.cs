// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the answer to a <see cref="ChoiceDecisionQuestion"/>.</summary>
/// <remarks>
/// Probabilities are reported by the provider and are not renormalized. Providers may round values,
/// so the distribution is not required to sum exactly to one.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class ChoiceDecisionAnswer : DecisionAnswer
{
    /// <summary>Initializes a new instance of the <see cref="ChoiceDecisionAnswer"/> class.</summary>
    /// <param name="questionId">The exact question ID.</param>
    /// <param name="selectedCandidateId">The exact ID of the selected candidate.</param>
    /// <param name="probabilities">The probability of each candidate, keyed by exact candidate ID.</param>
    /// <exception cref="ArgumentException"><paramref name="selectedCandidateId"/> is not present in <paramref name="probabilities"/>.</exception>
    [JsonConstructor]
    public ChoiceDecisionAnswer(string questionId, string selectedCandidateId, IReadOnlyList<DecisionProbability> probabilities)
        : base(questionId, DecisionKind.Choice)
    {
        SelectedCandidateId = Throw.IfNullOrWhitespace(selectedCandidateId);
        Probabilities = SnapshotDistribution(probabilities, nameof(probabilities));

        if (!Probabilities.Any(p => string.Equals(p.Id, selectedCandidateId, StringComparison.Ordinal)))
        {
            Throw.ArgumentException(nameof(selectedCandidateId), "The selected candidate must be present in the probability distribution.");
        }
    }

    /// <summary>Gets the exact ID of the selected candidate.</summary>
    public string SelectedCandidateId { get; }

    /// <summary>Gets the owned probability distribution over candidates.</summary>
    public IReadOnlyList<DecisionProbability> Probabilities { get; }
}
