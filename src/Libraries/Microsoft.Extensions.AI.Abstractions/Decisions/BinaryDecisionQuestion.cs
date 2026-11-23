// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Describes a binary decision question.</summary>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class BinaryDecisionQuestion : DecisionQuestion
{
    /// <summary>Initializes a new instance of the <see cref="BinaryDecisionQuestion"/> class.</summary>
    /// <param name="id">The opaque, caller-owned question ID.</param>
    /// <param name="instructions">The text instructions defining the proposition.</param>
    /// <param name="trueDescription">Optional text criteria describing a true result.</param>
    /// <param name="falseDescription">Optional text criteria describing a false result.</param>
    /// <exception cref="System.ArgumentException">
    /// Neither <paramref name="instructions"/> nor both descriptions were provided, or a provided description is empty.
    /// </exception>
    public BinaryDecisionQuestion(
        string id,
        string? instructions = null,
        string? trueDescription = null,
        string? falseDescription = null)
        : this(
            id,
            DecisionContents.FromText(instructions),
            DecisionContents.FromOptionalText(trueDescription, nameof(trueDescription)),
            DecisionContents.FromOptionalText(falseDescription, nameof(falseDescription)))
    {
    }

    /// <summary>Initializes a new instance of the <see cref="BinaryDecisionQuestion"/> class.</summary>
    /// <param name="id">The opaque, caller-owned question ID.</param>
    /// <param name="instructions">The instructions defining the proposition.</param>
    /// <param name="trueDescription">Optional criteria describing a true result.</param>
    /// <param name="falseDescription">Optional criteria describing a false result.</param>
    /// <exception cref="System.ArgumentException">
    /// Neither <paramref name="instructions"/> nor both descriptions were provided, or a provided description is empty.
    /// </exception>
    [JsonConstructor]
    public BinaryDecisionQuestion(
        string id,
        IList<AIContent>? instructions,
        IList<AIContent>? trueDescription = null,
        IList<AIContent>? falseDescription = null)
        : base(id, DecisionKind.Binary, DecisionContents.Snapshot(instructions, nameof(instructions)))
    {
        TrueDescription = DecisionContents.SnapshotOptional(trueDescription, nameof(trueDescription));
        FalseDescription = DecisionContents.SnapshotOptional(falseDescription, nameof(falseDescription));

        if (Instructions.Count == 0 && (TrueDescription is null || FalseDescription is null))
        {
            Throw.ArgumentException(
                nameof(instructions),
                "Binary questions require instructions or both true and false descriptions.");
        }
    }

    /// <summary>Gets the optional owned criteria describing a true result.</summary>
    /// <remarks>The list is a read-only snapshot. Attempting to modify it throws <see cref="System.NotSupportedException"/>.</remarks>
    public IList<AIContent>? TrueDescription { get; }

    /// <summary>Gets the optional owned criteria describing a false result.</summary>
    /// <remarks>The list is a read-only snapshot. Attempting to modify it throws <see cref="System.NotSupportedException"/>.</remarks>
    public IList<AIContent>? FalseDescription { get; }
}
