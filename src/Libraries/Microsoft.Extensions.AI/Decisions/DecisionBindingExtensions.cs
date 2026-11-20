// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extension methods for mapping a <see cref="DecisionResponse"/> to an application-owned result.</summary>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
public static class DecisionBindingExtensions
{
    /// <summary>Maps a decision response to an application-owned result.</summary>
    /// <typeparam name="TResult">The application-owned result type.</typeparam>
    /// <param name="response">The decision response.</param>
    /// <param name="map">The mapping delegate.</param>
    /// <returns>The application-owned result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="response"/> or <paramref name="map"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// Questions that the mapping does not read can be unanswered. Reading an unanswered question through
    /// <see cref="DecisionAnswerSet"/> throws <see cref="DecisionUnansweredException"/>.
    /// </remarks>
    public static TResult Bind<TResult>(this DecisionResponse response, Func<DecisionAnswerSet, TResult> map)
    {
        _ = Throw.IfNull(response);
        _ = Throw.IfNull(map);
        return map(new DecisionAnswerSet(response));
    }
}
