// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the response to a decision request.</summary>
/// <remarks>
/// <see cref="IDecisionClient"/> implementations return exactly one answer per question, in request order, with matching
/// question IDs and kinds.
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
}
