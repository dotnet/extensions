// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation;

/// <summary>
/// Extension methods for <see cref="EvaluationResult"/>.
/// </summary>
public static class EvaluationResultExtensions
{
    /// <summary>
    /// Adds the supplied <paramref name="diagnostics"/> to all <see cref="EvaluationMetric"/>s contained in the
    /// supplied <paramref name="result"/>.
    /// </summary>
    /// <param name="result">
    /// The <see cref="EvaluationResult"/> containing the <see cref="EvaluationMetric"/>s that are to be altered.
    /// </param>
    /// <param name="diagnostics">The <see cref="EvaluationDiagnostic"/>s that are to be added.</param>
    public static void AddDiagnosticsToAllMetrics(this EvaluationResult result, IEnumerable<EvaluationDiagnostic> diagnostics)
    {
        _ = Throw.IfNull(result);

        foreach (EvaluationMetric metric in result.Metrics.Values)
        {
            metric.AddDiagnostics(diagnostics);
        }
    }

    /// <summary>
    /// Adds the supplied <paramref name="diagnostics"/> to all <see cref="EvaluationMetric"/>s contained in the
    /// supplied <paramref name="result"/>.
    /// </summary>
    /// <param name="result">
    /// The <see cref="EvaluationResult"/> containing the <see cref="EvaluationMetric"/>s that are to be altered.
    /// </param>
    /// <param name="diagnostics">The <see cref="EvaluationDiagnostic"/>s that are to be added.</param>
    public static void AddDiagnosticsToAllMetrics(this EvaluationResult result, params EvaluationDiagnostic[] diagnostics)
        => AddDiagnosticsToAllMetrics(result, diagnostics as IEnumerable<EvaluationDiagnostic>);

    /// <summary>
    /// Returns <see langword="true"/> if any <see cref="EvaluationMetric"/> contained in the supplied
    /// <paramref name="result"/> contains an <see cref="EvaluationDiagnostic"/> matching the supplied
    /// <paramref name="predicate"/>; <see langword="false"/> otherwise.
    /// </summary>
    /// <param name="result">The <see cref="EvaluationResult"/> that is to be inspected.</param>
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
        this EvaluationResult result,
        Func<EvaluationDiagnostic, bool>? predicate = null)
    {
        _ = Throw.IfNull(result);

        return result.Metrics.Values.Any(m => m.ContainsDiagnostics(predicate));
    }

    /// <summary>
    /// Applies <see cref="EvaluationMetricInterpretation"/>s to one or more <see cref="EvaluationMetric"/>s contained
    /// in the supplied <paramref name="result"/>.
    /// </summary>
    /// <param name="result">
    /// The <see cref="EvaluationResult"/> containing the <see cref="EvaluationMetric"/>s that are to be interpreted.
    /// </param>
    /// <param name="interpretationProvider">
    /// A function that returns a new <see cref="EvaluationMetricInterpretation"/> that should be applied to the
    /// supplied <see cref="EvaluationMetric"/>, or <see langword="null"/> if the
    /// <see cref="EvaluationMetric.Interpretation"/> should be left unchanged.</param>
    public static void Interpret(
        this EvaluationResult result,
        Func<EvaluationMetric, EvaluationMetricInterpretation?> interpretationProvider)
    {
        _ = Throw.IfNull(result);
        _ = Throw.IfNull(interpretationProvider);

        foreach (EvaluationMetric metric in result.Metrics.Values)
        {
            if (interpretationProvider(metric) is EvaluationMetricInterpretation interpretation)
            {
                metric.Interpretation = interpretation;
            }
        }
    }
}
