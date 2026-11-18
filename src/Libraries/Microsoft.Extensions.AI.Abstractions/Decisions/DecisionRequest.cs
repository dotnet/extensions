// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the inputs to evaluate and a nonempty, ordered batch of questions.</summary>
/// <remarks>
/// <para>
/// Every question is evaluated against the same inputs: the optional JSON <see cref="State"/> and the ordered
/// <see cref="Contents"/>. At least one of them must be provided.
/// </para>
/// <para>
/// <see cref="Contents"/> and question instructions and criteria may contain any <see cref="AIContent"/>.
/// An <see cref="IDecisionClient"/> implementation must map every content item it can represent faithfully
/// and must throw <see cref="NotSupportedException"/> for any item it cannot represent. It must never silently
/// drop or alter content, because doing so changes the decision being requested.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class DecisionRequest
{
    /// <summary>Initializes a new instance of the <see cref="DecisionRequest"/> class and snapshots its inputs and questions.</summary>
    /// <param name="state">The optional JSON state evaluated by every question.</param>
    /// <param name="questions">The ordered heterogeneous question batch.</param>
    /// <param name="contents">The optional ordered content evaluated by every question.</param>
    /// <exception cref="ArgumentException">
    /// Neither <paramref name="state"/> nor nonempty <paramref name="contents"/> were provided, <paramref name="questions"/> is empty,
    /// or question IDs are not unique.
    /// </exception>
    [JsonConstructor]
    public DecisionRequest(JsonElement? state, IReadOnlyList<DecisionQuestion> questions, IReadOnlyList<AIContent>? contents = null)
    {
        if (state is { ValueKind: JsonValueKind.Undefined })
        {
            Throw.ArgumentException(nameof(state), "State must be explicit JSON.");
        }

        _ = Throw.IfNull(questions);

        DecisionQuestion[] copy = questions.ToArray();
        if (copy.Length == 0 || Array.Exists(copy, static question => question is null))
        {
            Throw.ArgumentException(nameof(questions), "Questions must be nonempty.");
        }

        HashSet<string> ids = new(StringComparer.Ordinal);
        foreach (DecisionQuestion question in copy)
        {
            if (!ids.Add(question.Id))
            {
                Throw.ArgumentException(nameof(questions), "Question IDs must be unique using ordinal comparison.");
            }
        }

        Contents = DecisionContents.Snapshot(contents, nameof(contents));
        if (state is null && Contents.Count == 0)
        {
            Throw.ArgumentException(nameof(state), "A request requires state, contents, or both.");
        }

        State = state?.Clone();
        Questions = Array.AsReadOnly(copy);
    }

    /// <summary>Gets the owned JSON state snapshot, or <see langword="null"/> when the request has no state.</summary>
    public JsonElement? State { get; }

    /// <summary>Gets the owned, ordered content snapshot.</summary>
    /// <remarks>The list is empty when the request has no content.</remarks>
    public IReadOnlyList<AIContent> Contents { get; }

    /// <summary>Gets the ordered, owned question snapshot.</summary>
    public IReadOnlyList<DecisionQuestion> Questions { get; }
}
