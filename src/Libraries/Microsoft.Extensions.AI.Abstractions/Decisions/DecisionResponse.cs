// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

#pragma warning disable S2302 // Messages mention "answer" and "question" as words, not parameter names.

/// <summary>Represents the response to a decision request.</summary>
/// <remarks>
/// A well-formed response contains exactly one answer per question, in request order. Use <see cref="Validate"/>
/// to verify that a response corresponds to the request that produced it.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
public class DecisionResponse
{
    private IList<DecisionAnswer>? _answers;

    /// <summary>Initializes a new instance of the <see cref="DecisionResponse"/> class.</summary>
    public DecisionResponse()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DecisionResponse"/> class.</summary>
    /// <param name="answers">The answers, one per question in request order.</param>
    [JsonConstructor]
    public DecisionResponse(IList<DecisionAnswer>? answers)
    {
        _answers = answers;
    }

    /// <summary>Gets or sets the answers, one per question in request order.</summary>
    [AllowNull]
    public IList<DecisionAnswer> Answers
    {
        get => _answers ??= new List<DecisionAnswer>();
        set => _answers = value;
    }

    /// <summary>Gets or sets the ID of the response, if the provider reports one.</summary>
    public string? ResponseId { get; set; }

    /// <summary>Gets or sets the ID of the model that produced the response.</summary>
    public string? ModelId { get; set; }

    /// <summary>Gets or sets usage details for the request.</summary>
    public UsageDetails? Usage { get; set; }

    /// <summary>Gets or sets the provider-owned raw representation of the response, if available.</summary>
    [JsonIgnore]
    public object? RawRepresentation { get; set; }

    /// <summary>Gets or sets provider-specific additional properties.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <summary>Gets the answer for the question with the specified exact ID.</summary>
    /// <param name="questionId">The exact question ID.</param>
    /// <returns>The answer associated with <paramref name="questionId"/>.</returns>
    /// <exception cref="KeyNotFoundException">No answer exists for <paramref name="questionId"/>.</exception>
    public DecisionAnswer GetAnswer(string questionId)
    {
        _ = Throw.IfNullOrWhitespace(questionId);
        if (_answers is not null)
        {
            foreach (DecisionAnswer answer in _answers)
            {
                if (answer is not null && string.Equals(answer.QuestionId, questionId, StringComparison.Ordinal))
                {
                    return answer;
                }
            }
        }

        throw new KeyNotFoundException($"No answer exists for question ID '{questionId}'.");
    }

    /// <summary>Verifies that this response corresponds to <paramref name="request"/>.</summary>
    /// <param name="request">The request that produced this response.</param>
    /// <remarks>
    /// A corresponding response contains exactly one answer per question, in request order, with matching question IDs and kinds.
    /// Each answer is either an <see cref="UnansweredDecisionAnswer"/> or the kind-specific answer type, and choice and score
    /// distributions contain exactly the requested candidate or level IDs, in order.
    /// </remarks>
    /// <exception cref="DecisionProtocolException">The response does not correspond to <paramref name="request"/>.</exception>
    public void Validate(DecisionRequest request)
    {
        _ = Throw.IfNull(request);

        int count = _answers?.Count ?? 0;
        if (count != request.Questions.Count)
        {
            throw new DecisionProtocolException("A response must contain exactly one answer for every question.");
        }

        for (int i = 0; i < count; i++)
        {
            ValidateAnswer(request.Questions[i], _answers![i]);
        }
    }

    private static void ValidateAnswer(DecisionQuestion question, DecisionAnswer? answer)
    {
        if (answer is null ||
            !string.Equals(question.Id, answer.QuestionId, StringComparison.Ordinal) ||
            question.Kind != answer.Kind)
        {
            throw new DecisionProtocolException($"The answer at the position of '{question.Id}' does not match its ID and kind.");
        }

        switch (question, answer)
        {
            case (_, UnansweredDecisionAnswer):
            case (BinaryDecisionQuestion, BinaryDecisionAnswer):
                return;

            case (ChoiceDecisionQuestion choice, ChoiceDecisionAnswer choiceAnswer):
                ValidateDistribution(question.Id, choiceAnswer.Probabilities, choice.Candidates.Count, i => choice.Candidates[i].Id);
                return;

            case (ScoreDecisionQuestion score, ScoreDecisionAnswer scoreAnswer):
                ValidateDistribution(question.Id, scoreAnswer.Probabilities, score.Levels.Count, i => score.Levels[i].Id);
                return;

            default:
                throw new DecisionProtocolException($"The answer type for '{question.Id}' does not match its kind.");
        }
    }

    private static void ValidateDistribution(
        string questionId,
        IReadOnlyList<DecisionProbability> probabilities,
        int expectedCount,
        Func<int, string> expectedId)
    {
        bool matches = probabilities.Count == expectedCount;
        for (int i = 0; matches && i < expectedCount; i++)
        {
            matches = string.Equals(probabilities[i].Id, expectedId(i), StringComparison.Ordinal);
        }

        if (!matches)
        {
            throw new DecisionProtocolException(
                $"The distribution for question '{questionId}' must contain exactly the requested IDs, in request order.");
        }
    }
}
