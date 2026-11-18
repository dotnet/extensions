// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the probability assigned to one caller-defined candidate or score level.</summary>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class DecisionProbability
{
    /// <summary>Initializes a new instance of the <see cref="DecisionProbability"/> class.</summary>
    /// <param name="id">The exact candidate or level ID.</param>
    /// <param name="value">The finite probability in the closed range [0, 1].</param>
    [JsonConstructor]
    public DecisionProbability(string id, double value)
    {
        Id = Throw.IfNullOrWhitespace(id);
        Value = DecisionAnswer.ValidateProbability(value, nameof(value));
    }

    /// <summary>Gets the exact candidate or level ID.</summary>
    public string Id { get; }

    /// <summary>Gets the probability in the closed range [0, 1].</summary>
    public double Value { get; }
}
