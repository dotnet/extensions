// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a typed decision result together with the decision response it was bound from.</summary>
/// <typeparam name="TResult">The application-owned result type.</typeparam>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class DecisionResponse<TResult>
{
    private readonly DecisionDefinition<TResult> _definition;

    internal DecisionResponse(TResult result, DecisionResponse response, DecisionDefinition<TResult> definition)
    {
        Result = result;
        Response = response;
        _definition = definition;
    }

    /// <summary>Gets the bound result.</summary>
    public TResult Result { get; }

    /// <summary>Gets the decision response the result was bound from.</summary>
    public DecisionResponse Response { get; }

    /// <summary>Gets the probability distribution of a choice-bound result property, keyed by enum value.</summary>
    /// <typeparam name="TEnum">The enum type of the result property.</typeparam>
    /// <param name="selector">A direct result property selector.</param>
    /// <returns>The probability of each candidate in the answer's distribution.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The selected property is not declared as a choice in the definition.</exception>
    public IReadOnlyDictionary<TEnum, double> GetDistribution<TEnum>(Expression<Func<TResult, TEnum>> selector)
        where TEnum : struct, Enum =>
        _definition.GetDistribution(Response, selector);
}
