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

/// <summary>Describes a choice decision question.</summary>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class ChoiceDecisionQuestion : DecisionQuestion
{
    /// <summary>Initializes a new instance of the <see cref="ChoiceDecisionQuestion"/> class.</summary>
    /// <param name="id">The opaque, caller-owned question ID.</param>
    /// <param name="instructions">The optional text instructions defining the choice.</param>
    /// <param name="candidates">The ordered, caller-defined candidates.</param>
    public ChoiceDecisionQuestion(string id, string? instructions, IReadOnlyList<DecisionCandidate> candidates)
        : this(id, DecisionContents.FromText(instructions), candidates)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ChoiceDecisionQuestion"/> class.</summary>
    /// <param name="id">The opaque, caller-owned question ID.</param>
    /// <param name="instructions">The optional instructions defining the choice.</param>
    /// <param name="candidates">The ordered, caller-defined candidates.</param>
    [JsonConstructor]
    public ChoiceDecisionQuestion(string id, IReadOnlyList<AIContent>? instructions, IReadOnlyList<DecisionCandidate> candidates)
        : base(id, DecisionKind.Choice, DecisionContents.Snapshot(instructions, nameof(instructions)))
    {
        _ = Throw.IfNull(candidates);

        DecisionCandidate[] copy = candidates.ToArray();
        if (copy.Length == 0 || Array.Exists(copy, static candidate => candidate is null))
        {
            Throw.ArgumentException(nameof(candidates), "Candidates must be nonempty.");
        }

        HashSet<string> ids = new(StringComparer.Ordinal);
        foreach (DecisionCandidate candidate in copy)
        {
            if (!ids.Add(candidate.Id))
            {
                Throw.ArgumentException(nameof(candidates), "Candidate IDs must be unique using ordinal comparison.");
            }
        }

        Candidates = Array.AsReadOnly(copy);
    }

    /// <summary>Gets the ordered, owned candidate snapshot.</summary>
    public IReadOnlyList<DecisionCandidate> Candidates { get; }
}
