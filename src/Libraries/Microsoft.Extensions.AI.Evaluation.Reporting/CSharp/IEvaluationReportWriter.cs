// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI.Evaluation.Reporting;

/// <summary>
/// Generates a report containing all the <see cref="EvaluationMetric"/>s present in the supplied
/// <see cref="ScenarioRunResult"/>s.
/// </summary>
public interface IEvaluationReportWriter
{
    /// <summary>
    /// Writes a report containing all the <see cref="EvaluationMetric"/>s present in the supplied
    /// <paramref name="scenarioRunResults"/>s.
    /// </summary>
    /// <param name="scenarioRunResults">An enumeration of <see cref="ScenarioRunResult"/>s.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can cancel the operation.</param>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation.</returns>
    ValueTask WriteReportAsync(
        IEnumerable<ScenarioRunResult> scenarioRunResults,
        CancellationToken cancellationToken = default);
}
