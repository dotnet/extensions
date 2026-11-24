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

/// <summary>Describes a score decision question.</summary>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class ScoreDecisionQuestion : DecisionQuestion
{
    /// <summary>Initializes a new instance of the <see cref="ScoreDecisionQuestion"/> class.</summary>
    /// <param name="id">The opaque, caller-owned question ID.</param>
    /// <param name="levels">The ordered, caller-defined score levels, from lowest to highest.</param>
    /// <param name="instructions">The optional text instructions defining the score.</param>
    public ScoreDecisionQuestion(string id, IReadOnlyList<DecisionScoreLevel> levels, string? instructions = null)
        : this(id, levels, DecisionContents.FromText(instructions))
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ScoreDecisionQuestion"/> class.</summary>
    /// <param name="id">The opaque, caller-owned question ID.</param>
    /// <param name="levels">The ordered, caller-defined score levels, from lowest to highest.</param>
    /// <param name="instructions">The optional instructions defining the score.</param>
    [JsonConstructor]
    public ScoreDecisionQuestion(string id, IReadOnlyList<DecisionScoreLevel> levels, IList<AIContent>? instructions)
        : base(id, DecisionKind.Score, DecisionContents.Snapshot(instructions, nameof(instructions)))
    {
        _ = Throw.IfNull(levels);

        DecisionScoreLevel[] copy = levels.ToArray();
        if (copy.Length < 2 || Array.Exists(copy, static level => level is null))
        {
            Throw.ArgumentException(nameof(levels), "A score question requires at least two levels.");
        }

        HashSet<string> ids = new(StringComparer.Ordinal);
        foreach (DecisionScoreLevel level in copy)
        {
            if (!ids.Add(level.Id))
            {
                Throw.ArgumentException(nameof(levels), "Score level IDs must be unique using ordinal comparison.");
            }
        }

        Levels = Array.AsReadOnly(copy);
    }

    /// <summary>Gets the ordered, owned score-level snapshot, from lowest to highest.</summary>
    public IReadOnlyList<DecisionScoreLevel> Levels { get; }
}
