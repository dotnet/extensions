// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a question that the provider did not answer, while other questions in the same request may have been answered.</summary>
/// <remarks>
/// <see cref="DecisionAnswer.Kind"/> is the kind of the unanswered question, so code that routes on kind still sees the
/// question's semantics. An unanswered question is not equivalent to a negative or low-valued answer.
/// Failures that affect the whole request are reported as exceptions instead.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class UnansweredDecisionAnswer : DecisionAnswer
{
    /// <summary>Initializes a new instance of the <see cref="UnansweredDecisionAnswer"/> class.</summary>
    /// <param name="questionId">The exact question ID.</param>
    /// <param name="kind">The kind of the unanswered question.</param>
    [JsonConstructor]
    public UnansweredDecisionAnswer(string questionId, DecisionKind kind)
        : base(questionId, kind)
    {
    }

    /// <summary>Gets or sets the reason the question was not answered, if known.</summary>
    public DecisionUnansweredReason? Reason { get; set; }
}
