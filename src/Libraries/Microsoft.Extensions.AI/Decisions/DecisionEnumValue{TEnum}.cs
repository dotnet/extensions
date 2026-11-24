// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Describes one entry of a <see cref="DecisionEnumDefinition{TEnum}"/>.</summary>
/// <typeparam name="TEnum">The application-owned enum type.</typeparam>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class DecisionEnumValue<TEnum>
    where TEnum : struct, Enum
{
    /// <summary>Initializes a new instance of the <see cref="DecisionEnumValue{TEnum}"/> class.</summary>
    /// <param name="value">The enum value.</param>
    /// <param name="id">The candidate ID.</param>
    /// <param name="description">The candidate description.</param>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> or <paramref name="description"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="id"/> or <paramref name="description"/> is empty or whitespace.</exception>
    public DecisionEnumValue(TEnum value, string id, string description)
    {
        Value = value;
        Id = Throw.IfNullOrWhitespace(id);
        Description = Throw.IfNullOrWhitespace(description);
    }

    /// <summary>Gets the enum value.</summary>
    public TEnum Value { get; }

    /// <summary>Gets the candidate ID.</summary>
    public string Id { get; }

    /// <summary>Gets the candidate description.</summary>
    public string Description { get; }
}
