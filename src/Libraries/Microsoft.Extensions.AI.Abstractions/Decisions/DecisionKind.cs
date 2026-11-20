// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Identifies the semantic kind of a decision question or answer.</summary>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
public enum DecisionKind
{
    /// <summary>A two-outcome proposition.</summary>
    Binary,

    /// <summary>A choice from a caller-defined unordered set.</summary>
    Choice,

    /// <summary>A position on a caller-defined ordered scale.</summary>
    Score,
}
