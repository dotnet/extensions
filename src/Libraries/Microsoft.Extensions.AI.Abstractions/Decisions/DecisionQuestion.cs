// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Describes a decision question.</summary>
/// <remarks>
/// Instructions and criteria are expressed as <see cref="AIContent"/> so that they can carry any content
/// a provider supports. Constructors that accept <see cref="string"/> values are conveniences that create a
/// single <see cref="TextContent"/>.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(BinaryDecisionQuestion), "binary")]
[JsonDerivedType(typeof(ChoiceDecisionQuestion), "choice")]
[JsonDerivedType(typeof(ScoreDecisionQuestion), "score")]
public abstract class DecisionQuestion
{
    private protected DecisionQuestion(string id, DecisionKind kind, IList<AIContent> instructions)
    {
        Id = Throw.IfNullOrWhitespace(id);
        Kind = kind;
        Instructions = instructions;
    }

    /// <summary>Gets the opaque, caller-owned correlation ID for this question.</summary>
    public string Id { get; }

    /// <summary>Gets the semantic kind of this question.</summary>
    public DecisionKind Kind { get; }

    /// <summary>Gets the owned, ordered instructions that define the question.</summary>
    /// <remarks>
    /// The list is a read-only snapshot and is empty when the question has no instructions.
    /// Attempting to modify it throws <see cref="System.NotSupportedException"/>.
    /// </remarks>
    public IList<AIContent> Instructions { get; }
}
