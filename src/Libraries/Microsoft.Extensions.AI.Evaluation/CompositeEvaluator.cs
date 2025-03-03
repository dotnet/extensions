// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.Evaluation.Utilities;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation;

/// <summary>
/// An <see cref="IEvaluator"/> that composes other <see cref="IEvaluator"/>s to execute multiple (concurrent)
/// evaluations on a supplied response.
/// </summary>
public sealed class CompositeEvaluator : IEvaluator
{
    /// <summary>
    /// Gets the <see cref="EvaluationMetric.Name"/>s of all the <see cref="EvaluationMetric"/>s produced by the
    /// composed <see cref="IEvaluator"/>s.
    /// </summary>
    public IReadOnlyCollection<string> EvaluationMetricNames { get; }

    private readonly IReadOnlyList<IEvaluator> _evaluators;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeEvaluator"/> class that composes the supplied
    /// <see cref="IEvaluator"/>s.
    /// </summary>
    /// <param name="evaluators">An array of <see cref="IEvaluator"/>s that are to be composed.</param>
    public CompositeEvaluator(params IEvaluator[] evaluators)
        : this(evaluators as IEnumerable<IEvaluator>)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeEvaluator"/> class that composes the supplied
    /// <see cref="IEvaluator"/>s.
    /// </summary>
    /// <param name="evaluators">An enumeration of <see cref="IEvaluator"/>s that are to be composed.</param>
    public CompositeEvaluator(IEnumerable<IEvaluator> evaluators)
    {
        _ = Throw.IfNull(evaluators, nameof(evaluators));

        var metricNames = new HashSet<string>();

        foreach (IEvaluator evaluator in evaluators)
        {
            if (evaluator.EvaluationMetricNames.Count == 0)
            {
#pragma warning disable S103 // Lines should not be too long
                throw new InvalidOperationException(
                    $"The '{nameof(evaluator.EvaluationMetricNames)}' property on '{evaluator.GetType().FullName}' returned an empty collection. An evaluator must advertise the names of the metrics that it supports.");
#pragma warning restore S103
            }

            foreach (string metricName in evaluator.EvaluationMetricNames)
            {
                if (!metricNames.Add(metricName))
                {
                    Throw.ArgumentException(nameof(evaluators), $"Cannot add multiple evaluators for '{metricName}'.");
                }
            }
        }

        EvaluationMetricNames = metricNames;
        _evaluators = [.. evaluators];
    }

    /// <summary>
    /// Evaluates the supplied <paramref name="modelResponse"/> and returns an <see cref="EvaluationResult"/>
    /// containing one or more <see cref="EvaluationMetric"/>s.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="EvaluationMetric.Name"/>s of the <see cref="EvaluationMetric"/>s contained in the returned
    /// <see cref="EvaluationResult"/> should match <see cref="EvaluationMetricNames"/>.
    /// </para>
    /// <para>
    /// Also note that <paramref name="chatConfiguration"/> must not be omitted if one or more composed
    /// <see cref="IEvaluator"/>s use an AI model to perform evaluation.
    /// </para>
    /// </remarks>
    /// <param name="messages">
    /// The conversation history including the request that produced the supplied <paramref name="modelResponse"/>.
    /// </param>
    /// <param name="modelResponse">The response that is to be evaluated.</param>
    /// <param name="chatConfiguration">
    /// A <see cref="ChatConfiguration"/> that specifies the <see cref="IChatClient"/> and the
    /// <see cref="IEvaluationTokenCounter"/> that should be used if one or more composed <see cref="IEvaluator"/>s use
    /// an AI model to perform evaluation.
    /// </param>
    /// <param name="additionalContext">
    /// Additional contextual information (beyond that which is available in <paramref name="messages"/>) that composed
    /// <see cref="IEvaluator"/>s may need to accurately evaluate the supplied <paramref name="modelResponse"/>.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can cancel the evaluation operation.
    /// </param>
    /// <returns>An <see cref="EvaluationResult"/> containing one or more <see cref="EvaluationMetric"/>s.</returns>
    public async ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatMessage modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        var metrics = new List<EvaluationMetric>();

        IAsyncEnumerable<EvaluationResult> resultsStream =
            EvaluateAndStreamResultsAsync(
                messages,
                modelResponse,
                chatConfiguration,
                additionalContext,
                cancellationToken);

        await foreach (EvaluationResult result in resultsStream.ConfigureAwait(false))
        {
            metrics.AddRange(result.Metrics.Values);
        }

        return new EvaluationResult(metrics);
    }

    private IAsyncEnumerable<EvaluationResult> EvaluateAndStreamResultsAsync(
        IEnumerable<ChatMessage> messages,
        ChatMessage modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        async ValueTask<EvaluationResult> EvaluateAsync(IEvaluator e)
        {
            try
            {
                return await e.EvaluateAsync(
                    messages,
                    modelResponse,
                    chatConfiguration,
                    additionalContext,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                string message = ex.ToString();
                EvaluationResult result = new EvaluationResult();

                if (e.EvaluationMetricNames.Count == 0)
                {
#pragma warning disable S103 // Lines should not be too long
                    throw new InvalidOperationException(
                        $"The '{nameof(e.EvaluationMetricNames)}' property on '{e.GetType().FullName}' returned an empty collection. An evaluator must advertise the names of the metrics that it supports.");
#pragma warning restore S103
                }

                foreach (string metricName in e.EvaluationMetricNames)
                {
                    var metric = new EvaluationMetric(metricName);
                    metric.AddDiagnostic(EvaluationDiagnostic.Error(message));
                    result.Metrics.Add(metric.Name, metric);
                }

                return result;
            }
        }

        IEnumerable<ValueTask<EvaluationResult>> concurrentTasks = _evaluators.Select(EvaluateAsync);
        return concurrentTasks.StreamResultsAsync(preserveOrder: false, cancellationToken);
    }
}
