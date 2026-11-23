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

/// <summary>Represents request-level inputs and a nonempty, ordered batch of questions evaluated against them.</summary>
/// <remarks>
/// <para>
/// <see cref="State"/> (optional JSON) and <see cref="Contents"/> (optional <see cref="AIContent"/> items) are both
/// request-level inputs. They are shared by every question in <see cref="Questions"/>.
/// </para>
/// <para>
/// A completed request must provide <see cref="State"/>, nonempty <see cref="Contents"/>, or both. Because both are
/// optional, settable properties, this type cannot enforce that invariant on its own; each <see cref="IDecisionClient"/>
/// implementation validates it when the request is executed.
/// </para>
/// <para>
/// <see cref="Contents"/> and question instructions and criteria may contain any <see cref="AIContent"/>.
/// An <see cref="IDecisionClient"/> implementation maps the inputs it supports to the provider's input format
/// and must throw <see cref="NotSupportedException"/> for any input it cannot represent, whether that is the
/// <see cref="State"/> or a content item. It must never silently drop or alter an input, because doing so changes
/// the decision being requested.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class DecisionRequest
{
    /// <summary>Initializes a new instance of the <see cref="DecisionRequest"/> class and snapshots its questions.</summary>
    /// <param name="questions">The ordered heterogeneous question batch.</param>
    /// <exception cref="ArgumentNullException"><paramref name="questions"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="questions"/> is empty, contains <see langword="null"/>, or question IDs are not unique.</exception>
    [JsonConstructor]
    public DecisionRequest(IReadOnlyList<DecisionQuestion> questions)
    {
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

        Questions = Array.AsReadOnly(copy);
    }

    /// <summary>Gets or sets the optional JSON state shared by every question in the request.</summary>
    /// <remarks>
    /// The assigned value is cloned, so the request owns its snapshot. <see langword="null"/> means the request has no state,
    /// which is distinct from a <see cref="JsonElement"/> whose <see cref="JsonElement.ValueKind"/> is <see cref="JsonValueKind.Null"/>.
    /// When serialized to JSON, both an absent state and an explicit JSON <c>null</c> state deserialize as <see langword="null"/>.
    /// An implementation that cannot represent the state throws <see cref="NotSupportedException"/>.
    /// </remarks>
    /// <exception cref="ArgumentException">The assigned value has <see cref="JsonValueKind.Undefined"/> kind.</exception>
    public JsonElement? State
    {
        get;
        set
        {
            if (value is { ValueKind: JsonValueKind.Undefined })
            {
                Throw.ArgumentException(nameof(value), "State must be explicit JSON.");
            }

            field = value?.Clone();
        }
    }

    /// <summary>Gets or sets the optional content shared by every question in the request.</summary>
    /// <remarks>
    /// Assigning a list copies it into an owned, read-only snapshot; the <see cref="AIContent"/> items themselves are shared.
    /// Although the property is typed as <see cref="IList{T}"/>, the returned list cannot be modified in place; attempting to
    /// do so throws <see cref="NotSupportedException"/>. Assign a new list to change the contents.
    /// Assigning <see langword="null"/> resets the contents to an empty list.
    /// An implementation throws <see cref="NotSupportedException"/> for any item it cannot represent.
    /// </remarks>
    /// <exception cref="ArgumentException">The assigned list contains a <see langword="null"/> item.</exception>
    [AllowNull]
    public IList<AIContent> Contents
    {
        get;
        set => field = DecisionContents.Snapshot(value, nameof(value));
    } = DecisionContents.Snapshot(null, "value");

    /// <summary>Gets the ordered, owned question snapshot.</summary>
    public IReadOnlyList<DecisionQuestion> Questions { get; }
}