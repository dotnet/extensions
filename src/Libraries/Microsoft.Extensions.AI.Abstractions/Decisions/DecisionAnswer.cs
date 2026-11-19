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

/// <summary>Represents the answer to one decision question.</summary>
/// <remarks>
/// A response contains exactly one answer per question, in request order. An answer is either a kind-specific
/// answer (<see cref="BinaryDecisionAnswer"/>, <see cref="ChoiceDecisionAnswer"/>, or <see cref="ScoreDecisionAnswer"/>)
/// or an <see cref="UnansweredDecisionAnswer"/> when the provider did not answer that question.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(BinaryDecisionAnswer), "binary")]
[JsonDerivedType(typeof(ChoiceDecisionAnswer), "choice")]
[JsonDerivedType(typeof(ScoreDecisionAnswer), "score")]
[JsonDerivedType(typeof(UnansweredDecisionAnswer), "unanswered")]
public abstract class DecisionAnswer
{
    private protected DecisionAnswer(string questionId, DecisionKind kind)
    {
        QuestionId = Throw.IfNullOrWhitespace(questionId);
        Kind = kind;
    }

    /// <summary>Gets the exact ID of the question this answer belongs to.</summary>
    public string QuestionId { get; }

    /// <summary>Gets the kind of the question this answer belongs to.</summary>
    public DecisionKind Kind { get; }

    /// <summary>Gets or sets the provider-owned raw representation of the answer, if available.</summary>
    [JsonIgnore]
    public object? RawRepresentation { get; set; }

    /// <summary>Gets or sets provider-specific additional properties, such as native confidence values.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    internal static double ValidateProbability(double value, string paramName)
    {
        if (double.IsNaN(value) || value < 0d || value > 1d)
        {
            Throw.ArgumentOutOfRangeException(paramName, value, "Probabilities must be finite values in the closed range [0, 1].");
        }

        return value;
    }

    internal static IReadOnlyList<DecisionProbability> SnapshotDistribution(
        IReadOnlyList<DecisionProbability> probabilities,
        string paramName)
    {
        _ = Throw.IfNull(probabilities, paramName);

        DecisionProbability[] copy = probabilities.ToArray();
        if (Array.Exists(copy, static probability => probability is null))
        {
            Throw.ArgumentException(paramName, "Probabilities must not contain null entries.");
        }

        HashSet<string> ids = new(StringComparer.Ordinal);
        foreach (DecisionProbability probability in copy)
        {
            if (!ids.Add(probability.Id))
            {
                Throw.ArgumentException(paramName, "Probability IDs must be unique using ordinal comparison.");
            }
        }

        return Array.AsReadOnly(copy);
    }
}
