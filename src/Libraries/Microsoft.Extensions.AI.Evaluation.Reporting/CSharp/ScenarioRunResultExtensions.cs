// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.Reporting;

/// <summary>
/// Extension methods for <see cref="ScenarioRunResult"/>.
/// </summary>
public static class ScenarioRunResultExtensions
{
    /// <summary>
    /// Returns <see langword="true"/> if any <see cref="EvaluationMetric"/> contained in the supplied
    /// <paramref name="result"/> contains an <see cref="EvaluationDiagnostic"/> matching the supplied
    /// <paramref name="predicate"/>; <see langword="false"/> otherwise.
    /// </summary>
    /// <param name="result">The <see cref="ScenarioRunResult"/> that is to be inspected.</param>
    /// <param name="predicate">
    /// A predicate that returns <see langword="true"/> if a matching <see cref="EvaluationDiagnostic"/> is found;
    /// <see langword="false"/> otherwise.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if any <see cref="EvaluationMetric"/> contained in the supplied
    /// <paramref name="result"/> contains an <see cref="EvaluationDiagnostic"/> matching the supplied
    /// <paramref name="predicate"/>; <see langword="false"/> otherwise.
    /// </returns>
    public static bool ContainsDiagnostics(
        this ScenarioRunResult result,
        Func<EvaluationDiagnostic, bool>? predicate = null)
    {
        _ = Throw.IfNull(result, nameof(result));

        return result.EvaluationResult.ContainsDiagnostics(predicate);
    }
}
