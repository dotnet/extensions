// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the answer to a <see cref="BinaryDecisionQuestion"/>.</summary>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class BinaryDecisionAnswer : DecisionAnswer
{
    /// <summary>Initializes a new instance of the <see cref="BinaryDecisionAnswer"/> class.</summary>
    /// <param name="questionId">The exact question ID.</param>
    /// <param name="trueProbability">The probability, in the closed range [0, 1], that the proposition is true.</param>
    [JsonConstructor]
    public BinaryDecisionAnswer(string questionId, double trueProbability)
        : base(questionId, DecisionKind.Binary)
    {
        TrueProbability = ValidateProbability(trueProbability, nameof(trueProbability));
    }

    /// <summary>Gets the probability, in the closed range [0, 1], that the proposition is true.</summary>
    public double TrueProbability { get; }
}
