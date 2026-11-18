// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Describes one caller-defined choice candidate.</summary>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class DecisionCandidate
{
    /// <summary>Initializes a new instance of the <see cref="DecisionCandidate"/> class.</summary>
    /// <param name="id">The exact opaque candidate ID to round-trip in results.</param>
    /// <param name="description">The text criteria for the candidate.</param>
    public DecisionCandidate(string id, string description)
        : this(id, DecisionContents.FromText(Throw.IfNullOrWhitespace(description)))
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DecisionCandidate"/> class.</summary>
    /// <param name="id">The exact opaque candidate ID to round-trip in results.</param>
    /// <param name="description">The nonempty criteria for the candidate.</param>
    [JsonConstructor]
    public DecisionCandidate(string id, IReadOnlyList<AIContent> description)
    {
        Id = Throw.IfNullOrWhitespace(id);
        Description = DecisionContents.SnapshotRequired(description, nameof(description));
    }

    /// <summary>Gets the exact opaque, caller-owned candidate ID.</summary>
    public string Id { get; }

    /// <summary>Gets the owned criteria for the candidate.</summary>
    public IReadOnlyList<AIContent> Description { get; }
}
