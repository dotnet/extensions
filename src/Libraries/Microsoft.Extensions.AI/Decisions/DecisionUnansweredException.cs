// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

#pragma warning disable CA1032 // Implement standard exception constructors
#pragma warning disable RCS1194 // Implement exception constructors

namespace Microsoft.Extensions.AI;

/// <summary>
/// The exception that is thrown when a typed decision result requires an answer that the provider left unanswered.
/// </summary>
/// <remarks>
/// The complete response, including every <see cref="UnansweredDecisionAnswer"/>, is available from <see cref="Response"/>.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class DecisionUnansweredException : InvalidOperationException
{
    /// <summary>Initializes a new instance of the <see cref="DecisionUnansweredException"/> class.</summary>
    /// <param name="response">The decision response.</param>
    /// <param name="questionIds">The IDs of the required questions that were not answered.</param>
    /// <exception cref="ArgumentNullException"><paramref name="response"/> or <paramref name="questionIds"/> is <see langword="null"/>.</exception>
    public DecisionUnansweredException(DecisionResponse response, IEnumerable<string> questionIds)
        : this(Throw.IfNull(response), Throw.IfNull(questionIds).ToArray())
    {
    }

    private DecisionUnansweredException(DecisionResponse response, string[] questionIds)
        : base($"The decision response did not answer required question(s): {string.Join(", ", questionIds.Select(static id => $"'{id}'"))}.")
    {
        Response = response;
        QuestionIds = Array.AsReadOnly(questionIds);
    }

    /// <summary>Gets the decision response.</summary>
    public DecisionResponse Response { get; }

    /// <summary>Gets the IDs of the required questions that were not answered.</summary>
    public IReadOnlyList<string> QuestionIds { get; }
}
