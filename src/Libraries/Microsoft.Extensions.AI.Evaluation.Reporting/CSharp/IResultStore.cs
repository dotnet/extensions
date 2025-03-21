// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI.Evaluation.Reporting;

/// <summary>
/// Represents a store for <see cref="ScenarioRunResult"/>s.
/// </summary>
public interface IResultStore
{
    /// <summary>
    /// Returns <see cref="ScenarioRunResult"/>s for <see cref="ScenarioRun"/>s filtered by the specified
    /// <paramref name="executionName"/>, <paramref name="scenarioName"/>, and <paramref name="iterationName"/> from
    /// the store.
    /// </summary>
    /// <remarks>
    /// Returns all <see cref="ScenarioRunResult"/>s in the store if <paramref name="executionName"/>,
    /// <paramref name="scenarioName"/>, and <paramref name="iterationName"/> are all omitted.
    /// </remarks>
    /// <param name="executionName">
    /// The <see cref="ScenarioRun.ExecutionName"/> by which the <see cref="ScenarioRunResult"/>s should be filtered.
    /// If omitted, all <see cref="ScenarioRun.ExecutionName"/>s are considered.
    /// </param>
    /// <param name="scenarioName">
    /// The <see cref="ScenarioRun.ScenarioName"/> by which the <see cref="ScenarioRunResult"/>s should be filtered.
    /// If omitted, all <see cref="ScenarioRun.ScenarioName"/>s that are in scope based on the specified
    /// <paramref name="executionName"/> filter are considered.
    /// </param>
    /// <param name="iterationName">
    /// The <see cref="ScenarioRun.IterationName"/> by which the <see cref="ScenarioRunResult"/>s should be filtered.
    /// If omitted, all <see cref="ScenarioRun.IterationName"/>s that are in scope based on the specified
    /// <paramref name="executionName"/>, and <paramref name="scenarioName"/> filters are considered.
    /// </param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can cancel the operation.</param>
    /// <returns>The matching <see cref="ScenarioRunResult"/>s.</returns>
    IAsyncEnumerable<ScenarioRunResult> ReadResultsAsync(
        string? executionName = null,
        string? scenarioName = null,
        string? iterationName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes the supplied <paramref name="results"/>s to the store.
    /// </summary>
    /// <param name="results">The <see cref="ScenarioRunResult"/>s to be written.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can cancel the operation.</param>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation.</returns>
    ValueTask WriteResultsAsync(IEnumerable<ScenarioRunResult> results, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes <see cref="ScenarioRunResult"/>s for <see cref="ScenarioRun"/>s filtered by the specified
    /// <paramref name="executionName"/>, <paramref name="scenarioName"/>, and <paramref name="iterationName"/> from
    /// the store.
    /// </summary>
    /// <remarks>
    /// Deletes all <see cref="ScenarioRunResult"/>s in the store if <paramref name="executionName"/>,
    /// <paramref name="scenarioName"/>, and <paramref name="iterationName"/> are all omitted.
    /// </remarks>
    /// <param name="executionName">
    /// The <see cref="ScenarioRun.ExecutionName"/> by which the <see cref="ScenarioRunResult"/>s should be filtered.
    /// If omitted, all <see cref="ScenarioRun.ExecutionName"/>s are considered.
    /// </param>
    /// <param name="scenarioName">
    /// The <see cref="ScenarioRun.ScenarioName"/> by which the <see cref="ScenarioRunResult"/>s should be filtered.
    /// If omitted, all <see cref="ScenarioRun.ScenarioName"/>s that are in scope based on the specified
    /// <paramref name="executionName"/> filter are considered.
    /// </param>
    /// <param name="iterationName">
    /// The <see cref="ScenarioRun.IterationName"/> by which the <see cref="ScenarioRunResult"/>s should be filtered.
    /// If omitted, all <see cref="ScenarioRun.IterationName"/>s that are in scope based on the specified
    /// <paramref name="executionName"/>, and <paramref name="scenarioName"/> filters are considered.
    /// </param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can cancel the operation.</param>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation.</returns>
    ValueTask DeleteResultsAsync(
        string? executionName = null,
        string? scenarioName = null,
        string? iterationName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the <see cref="ScenarioRun.ExecutionName"/>s of the most recent <paramref name="count"/> executions from
    /// the store (ordered from most recent to least recent).
    /// </summary>
    /// <param name="count">The number of <see cref="ScenarioRun.ExecutionName"/>s to retrieve.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can cancel the operation.</param>
    /// <returns>
    /// The <see cref="ScenarioRun.ExecutionName"/>s of the most recent <paramref name="count"/> executions from the
    /// store (ordered from most recent to least recent).
    /// </returns>
    IAsyncEnumerable<string> GetLatestExecutionNamesAsync(
        int? count = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the <see cref="ScenarioRun.ScenarioName"/>s present in the execution with the specified
    /// <paramref name="executionName"/>.
    /// </summary>
    /// <param name="executionName">The <see cref="ScenarioRun.ExecutionName"/>.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can cancel the operation.</param>
    /// <returns>
    /// The <see cref="ScenarioRun.ScenarioName"/>s present in the execution with the specified
    /// <paramref name="executionName"/>.
    /// </returns>
    IAsyncEnumerable<string> GetScenarioNamesAsync(
        string executionName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the <see cref="ScenarioRun.IterationName"/>s present in the scenario with the specified
    /// <paramref name="scenarioName"/> under the execution with the specified <paramref name="executionName"/>.
    /// </summary>
    /// <param name="executionName">The <see cref="ScenarioRun.ExecutionName"/>.</param>
    /// <param name="scenarioName">The <see cref="ScenarioRun.ScenarioName"/>.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can cancel the operation.</param>
    /// <returns>
    /// The <see cref="ScenarioRun.IterationName"/>s present in the scenario with the specified
    /// <paramref name="scenarioName"/> under the execution with the specified <paramref name="executionName"/>.
    /// </returns>
    IAsyncEnumerable<string> GetIterationNamesAsync(
        string executionName,
        string scenarioName,
        CancellationToken cancellationToken = default);
}
