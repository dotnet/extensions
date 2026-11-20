// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides typed access to the answers of a decision response.</summary>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class DecisionAnswerSet
{
    internal DecisionAnswerSet(DecisionResponse response)
    {
        Response = response;
    }

    /// <summary>Gets the underlying decision response.</summary>
    public DecisionResponse Response { get; }

    /// <summary>Gets a binary answer by question ID.</summary>
    /// <param name="questionId">The exact question ID.</param>
    /// <returns>The binary answer.</returns>
    /// <exception cref="DecisionUnansweredException">The question was not answered.</exception>
    /// <exception cref="InvalidOperationException">The answer is not a binary answer.</exception>
    public BinaryDecisionAnswer GetBinary(string questionId) => GetAnswer<BinaryDecisionAnswer>(questionId, "binary");

    /// <summary>Gets a choice answer by question ID.</summary>
    /// <param name="questionId">The exact question ID.</param>
    /// <returns>The choice answer.</returns>
    /// <exception cref="DecisionUnansweredException">The question was not answered.</exception>
    /// <exception cref="InvalidOperationException">The answer is not a choice answer.</exception>
    public ChoiceDecisionAnswer GetChoice(string questionId) => GetAnswer<ChoiceDecisionAnswer>(questionId, "a choice");

    /// <summary>Gets a choice answer by question ID and maps its selected candidate ID to an enum value.</summary>
    /// <typeparam name="TEnum">The application-owned enum type.</typeparam>
    /// <param name="questionId">The exact question ID.</param>
    /// <param name="definition">The enum mapping.</param>
    /// <returns>The mapped enum value.</returns>
    /// <exception cref="DecisionUnansweredException">The question was not answered.</exception>
    /// <exception cref="InvalidOperationException">The answer is not a choice answer, or its selected candidate is not mapped.</exception>
    public TEnum GetChoice<TEnum>(string questionId, DecisionEnumDefinition<TEnum> definition)
        where TEnum : struct, Enum
    {
        _ = Throw.IfNull(definition);
        return definition.Parse(GetChoice(questionId).SelectedCandidateId);
    }

    /// <summary>Gets a score answer by question ID.</summary>
    /// <param name="questionId">The exact question ID.</param>
    /// <returns>The score answer.</returns>
    /// <exception cref="DecisionUnansweredException">The question was not answered.</exception>
    /// <exception cref="InvalidOperationException">The answer is not a score answer.</exception>
    public ScoreDecisionAnswer GetScore(string questionId) => GetAnswer<ScoreDecisionAnswer>(questionId, "a score");

    private TAnswer GetAnswer<TAnswer>(string questionId, string kind)
        where TAnswer : DecisionAnswer =>
        Response.GetAnswer(questionId) switch
        {
            TAnswer answer => answer,
            UnansweredDecisionAnswer => throw new DecisionUnansweredException(Response, [questionId]),
            _ => throw new InvalidOperationException($"Question '{questionId}' is not {kind}."),
        };
}
