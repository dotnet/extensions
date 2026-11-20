// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Provides per-request decision options.</summary>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
public class DecisionOptions
{
    /// <summary>Initializes a new instance of the <see cref="DecisionOptions"/> class.</summary>
    public DecisionOptions()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DecisionOptions"/> class by copying another instance.</summary>
    /// <param name="other">The instance to copy, or <see langword="null"/>.</param>
    protected DecisionOptions(DecisionOptions? other)
    {
        if (other is null)
        {
            return;
        }

        ModelId = other.ModelId;
        AdditionalProperties = other.AdditionalProperties?.Clone();
        RawRepresentationFactory = other.RawRepresentationFactory;
    }

    /// <summary>Gets or sets the requested model ID.</summary>
    public string? ModelId { get; set; }

    /// <summary>Gets or sets provider-specific additional properties.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <summary>
    /// Gets or sets a callback that creates provider-specific raw options.
    /// The callback should return a new instance for each invocation; the provider may mutate it.
    /// </summary>
    [JsonIgnore]
    public Func<IDecisionClient, object?>? RawRepresentationFactory { get; set; }

    /// <summary>Creates a shallow clone of these options.</summary>
    /// <returns>A cloned options instance.</returns>
    public virtual DecisionOptions Clone() => new(this);
}
