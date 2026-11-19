// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides an explicit mapping between choice candidate IDs and enum values.</summary>
/// <typeparam name="TEnum">The application-owned enum type.</typeparam>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class DecisionEnumDefinition<TEnum>
    where TEnum : struct, Enum
{
    private readonly ReadOnlyDictionary<string, TEnum> _values;

    /// <summary>Initializes a new instance of the <see cref="DecisionEnumDefinition{TEnum}"/> class.</summary>
    /// <param name="values">The ordered enum mapping entries.</param>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="values"/> is empty, or contains duplicate enum values or candidate IDs.
    /// </exception>
    public DecisionEnumDefinition(IEnumerable<DecisionEnumValue<TEnum>> values)
    {
        _ = Throw.IfNull(values);

        DecisionEnumValue<TEnum>[] copy = values.ToArray();
        if (copy.Length == 0)
        {
            Throw.ArgumentException(nameof(values), "An enum definition requires at least one value.");
        }

        Dictionary<string, TEnum> byId = new(StringComparer.Ordinal);
        HashSet<TEnum> enumValues = [];
        DecisionCandidate[] candidates = new DecisionCandidate[copy.Length];

        for (int i = 0; i < copy.Length; i++)
        {
            DecisionEnumValue<TEnum> value = Throw.IfNull(copy[i], nameof(values));
            if (!enumValues.Add(value.Value))
            {
                Throw.ArgumentException(nameof(values), "Enum aliases or duplicate enum values are not allowed.");
            }

            if (byId.ContainsKey(value.Id))
            {
                Throw.ArgumentException(nameof(values), "Enum candidate IDs must be unique using ordinal comparison.");
            }

            byId.Add(value.Id, value.Value);
            candidates[i] = new DecisionCandidate(value.Id, value.Description);
        }

        _values = new ReadOnlyDictionary<string, TEnum>(byId);
        Candidates = Array.AsReadOnly(candidates);
    }

    /// <summary>Gets the ordered candidates for a choice question.</summary>
    public IReadOnlyList<DecisionCandidate> Candidates { get; }

    /// <summary>Creates a choice question whose candidates are this mapping's candidates.</summary>
    /// <param name="id">The question ID.</param>
    /// <param name="instructions">Optional question instructions.</param>
    /// <returns>The choice question.</returns>
    public ChoiceDecisionQuestion CreateQuestion(string id, string? instructions = null) =>
        new(id, instructions, Candidates);

    /// <summary>Maps a candidate ID to its enum value.</summary>
    /// <param name="candidateId">The exact candidate ID.</param>
    /// <returns>The mapped enum value.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="candidateId"/> is not a candidate of this mapping.</exception>
    public TEnum Parse(string candidateId)
    {
        _ = Throw.IfNullOrWhitespace(candidateId);
        if (_values.TryGetValue(candidateId, out TEnum value))
        {
            return value;
        }

        throw new InvalidOperationException($"Unknown candidate ID '{candidateId}'.");
    }
}
