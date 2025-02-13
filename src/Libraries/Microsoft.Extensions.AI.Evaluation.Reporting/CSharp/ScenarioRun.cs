// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI.Evaluation.Reporting;

/// <summary>
/// Represents a single execution of a particular iteration of a particular scenario under evaluation.
/// </summary>
/// <remarks>
/// Each execution of an evaluation run is assigned a unique <see cref="ExecutionName"/>. A single such evaluation run
/// can contain evaluations for multiple scenarios each with a unique <see cref="ScenarioName"/>. The execution of each
/// such scenario in turn can include multiple iterations each with a unique <see cref="IterationName"/>.
/// </remarks>
public sealed class ScenarioRun : IAsyncDisposable
{
    /// <summary>
    /// Gets the name of the scenario that this <see cref="ScenarioRun"/> represents.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="ScenarioName"/>s of different scenarios within a particular evaluation run must be unique.
    /// </para>
    /// <para>
    /// Logically, a scenario can be mapped to a single unit test within a suite of unit tests that are executed as
    /// part of an evaluation. In this case, the <see cref="ScenarioName"/> for each <see cref="ScenarioRun"/> in the
    /// suite can be set to the fully qualified name of the corresponding unit test.
    /// </para>
    /// </remarks>
    public string ScenarioName { get; }

    /// <summary>
    /// Gets the name of the iteration that this <see cref="ScenarioRun"/> represents.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="IterationName"/>s of different iterations within a particular scenario execution must be unique.
    /// </para>
    /// <para>
    /// Logically, an iteration can be mapped to a single loop iteration within a particular unit test, or to a single
    /// data row within a data-driven test. <see cref="IterationName"/> could be set to any string that uniquely
    /// identifies the particular loop iteration / data row. For example, it could be set to an integer index that is
    /// incremented with each loop iteration.
    /// </para>
    /// </remarks>
    public string IterationName { get; }

    /// <summary>
    /// Gets the name of the execution that this <see cref="ScenarioRun"/> represents.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="ExecutionName"/> can be set to any string that uniquely identifies a particular execution of a set
    /// scenarios and iterations that are part of an evaluation run. For example, <see cref="ExecutionName"/> could be
    /// set to the build number of the GitHub Actions workflow that runs the evaluation. Or it could be set to the
    /// version number of the product being evaluated. It could also be set to a timestamp (so long as all
    /// <see cref="ScenarioRun"/>s in a particular evaluation run share the same timestamp for their
    /// <see cref="ExecutionName"/>s).
    /// </para>
    /// <para>
    /// As new builds / workflows are kicked off over time, this would produce a series of executions each with a
    /// unique <see cref="ExecutionName"/>. The results for individual scenarios and iterations can then be compared
    /// across these different executions to track how the <see cref="EvaluationMetric"/>s for each scenario and
    /// iteration are trending over time.
    /// </para>
    /// <para>
    /// If the supplied <see cref="ExecutionName"/> is not unique, then the results for the scenarios and iterations
    /// from the previous execution with the same <see cref="ExecutionName"/> will be overwritten with the results from
    /// the new execution.
    /// </para>
    /// </remarks>
    public string ExecutionName { get; }

    /// <summary>
    /// Gets a <see cref="Evaluation.ChatConfiguration"/> that specifies the <see cref="IChatClient"/> and the
    /// <see cref="IEvaluationTokenCounter"/> that are used by AI-based <see cref="IEvaluator"/>s that are invoked as
    /// part of the evaluation of this <see cref="ScenarioRun"/>.
    /// </summary>
    public ChatConfiguration? ChatConfiguration { get; }

    private readonly CompositeEvaluator _compositeEvaluator;
    private readonly IResultStore _resultStore;
    private readonly Func<EvaluationMetric, EvaluationMetricInterpretation?>? _evaluationMetricInterpreter;

    private ScenarioRunResult? _result;

    internal ScenarioRun(
        string scenarioName,
        string iterationName,
        string executionName,
        IEnumerable<IEvaluator> evaluators,
        IResultStore resultStore,
        ChatConfiguration? chatConfiguration = null,
        Func<EvaluationMetric, EvaluationMetricInterpretation?>? evaluationMetricInterpreter = null)
    {
        ScenarioName = scenarioName;
        IterationName = iterationName;
        ExecutionName = executionName;
        ChatConfiguration = chatConfiguration;

        _compositeEvaluator = new CompositeEvaluator(evaluators);
        _resultStore = resultStore;
        _evaluationMetricInterpreter = evaluationMetricInterpreter;
    }

    /// <summary>
    /// Evaluates the supplied <paramref name="modelResponse"/> and returns an <see cref="EvaluationResult"/>
    /// containing one or more <see cref="EvaluationMetric"/>s.
    /// </summary>
    /// <param name="messages">
    /// The conversation history including the request that produced the supplied <paramref name="modelResponse"/>.
    /// </param>
    /// <param name="modelResponse">The response that is to be evaluated.</param>
    /// <param name="additionalContext">
    /// Additional contextual information (beyond that which is available in <paramref name="messages"/>) that the
    /// <see cref="IEvaluator"/>s included in this <see cref="ScenarioRun"/> may need to accurately evaluate the
    /// supplied <paramref name="modelResponse"/>.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can cancel the evaluation operation.
    /// </param>
    /// <returns>An <see cref="EvaluationResult"/> containing one or more <see cref="EvaluationMetric"/>s.</returns>
    public async ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatMessage modelResponse,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        if (_result is not null)
        {
#pragma warning disable S103 // Lines should not be too long
            throw new InvalidOperationException(
                $"The {nameof(ScenarioRun)} with {nameof(ScenarioName)}: {ScenarioName}, {nameof(IterationName)}: {IterationName} and {nameof(ExecutionName)}: {ExecutionName} has already been evaluated. Do not call {nameof(EvaluateAsync)} more than once on a given {nameof(ScenarioRun)}.");
#pragma warning restore S103
        }

        EvaluationResult evaluationResult =
            await _compositeEvaluator.EvaluateAsync(
                messages,
                modelResponse,
                ChatConfiguration,
                additionalContext,
                cancellationToken).ConfigureAwait(false);

        if (_evaluationMetricInterpreter is not null)
        {
            evaluationResult.Interpret(_evaluationMetricInterpreter);
        }

        _result =
            new ScenarioRunResult(
                ScenarioName,
                IterationName,
                ExecutionName,
                creationTime: DateTime.UtcNow,
                messages,
                modelResponse,
                evaluationResult);

        return evaluationResult;
    }

    /// <summary>
    /// Disposes the <see cref="ScenarioRun"/> and writes the <see cref="ScenarioRunResult"/> to the configured
    /// <see cref="IResultStore"/>.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_result is not null)
        {
            await _resultStore.WriteResultsAsync([_result]).ConfigureAwait(false);
        }
    }
}
