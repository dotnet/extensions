// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Describes one level in an ordered score rubric.</summary>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class DecisionScoreLevel
{
    /// <summary>Initializes a new instance of the <see cref="DecisionScoreLevel"/> class.</summary>
    /// <param name="id">The exact opaque, caller-owned level ID.</param>
    /// <param name="description">The text criteria for this level.</param>
    public DecisionScoreLevel(string id, string description)
        : this(id, DecisionContents.FromText(Throw.IfNullOrWhitespace(description)))
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DecisionScoreLevel"/> class.</summary>
    /// <param name="id">The exact opaque, caller-owned level ID.</param>
    /// <param name="description">The nonempty criteria for this level.</param>
    [JsonConstructor]
    public DecisionScoreLevel(string id, IReadOnlyList<AIContent> description)
    {
        Id = Throw.IfNullOrWhitespace(id);
        Description = DecisionContents.SnapshotRequired(description, nameof(description));
    }

    /// <summary>Gets the exact opaque level ID.</summary>
    public string Id { get; }

    /// <summary>Gets the owned criteria for this level.</summary>
    public IReadOnlyList<AIContent> Description { get; }
}
