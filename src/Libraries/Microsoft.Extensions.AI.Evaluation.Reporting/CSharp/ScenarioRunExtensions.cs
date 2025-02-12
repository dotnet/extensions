// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.Reporting;

/// <summary>
/// Extension methods for <see cref="ScenarioRun"/>.
/// </summary>
public static class ScenarioRunExtensions
{
    /// <summary>
    /// Evaluates the supplied <paramref name="modelResponse"/> and returns an <see cref="EvaluationResult"/>
    /// containing one or more <see cref="EvaluationMetric"/>s.
    /// </summary>
    /// <param name="scenarioRun">The <see cref="ScenarioRun"/> of which this evaluation is a part.</param>
    /// <param name="modelResponse">The response that is to be evaluated.</param>
    /// <param name="additionalContext">
    /// Additional contextual information that the <see cref="IEvaluator"/>s included in this <see cref="ScenarioRun"/>
    /// may need to accurately evaluate the supplied <paramref name="modelResponse"/>.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can cancel the evaluation operation.
    /// </param>
    /// <returns>An <see cref="EvaluationResult"/> containing one or more <see cref="EvaluationMetric"/>s.</returns>
    public static ValueTask<EvaluationResult> EvaluateAsync(
        this ScenarioRun scenarioRun,
        string modelResponse,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default) =>
            scenarioRun.EvaluateAsync(
                modelResponse: new ChatMessage(ChatRole.Assistant, modelResponse),
                additionalContext: additionalContext,
                cancellationToken: cancellationToken);

    /// <summary>
    /// Evaluates the supplied <paramref name="modelResponse"/> and returns an <see cref="EvaluationResult"/>
    /// containing one or more <see cref="EvaluationMetric"/>s.
    /// </summary>
    /// <param name="scenarioRun">The <see cref="ScenarioRun"/> of which this evaluation is a part.</param>
    /// <param name="userRequest">
    /// The request that produced the <paramref name="modelResponse"/> that is to be evaluated.
    /// </param>
    /// <param name="modelResponse">The response that is to be evaluated.</param>
    /// <param name="additionalContext">
    /// Additional contextual information (beyond that which is available in <paramref name="userRequest"/>) that the
    /// <see cref="IEvaluator"/>s included in this <see cref="ScenarioRun"/> may need to accurately evaluate the
    /// supplied <paramref name="modelResponse"/>.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can cancel the evaluation operation.
    /// </param>
    /// <returns>An <see cref="EvaluationResult"/> containing one or more <see cref="EvaluationMetric"/>s.</returns>
    public static ValueTask<EvaluationResult> EvaluateAsync(
        this ScenarioRun scenarioRun,
        string userRequest,
        string modelResponse,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default) =>
            scenarioRun.EvaluateAsync(
                userRequest: new ChatMessage(ChatRole.User, userRequest),
                modelResponse: new ChatMessage(ChatRole.Assistant, modelResponse),
                additionalContext: additionalContext,
                cancellationToken: cancellationToken);

    /// <summary>
    /// Evaluates the supplied <paramref name="modelResponse"/> and returns an <see cref="EvaluationResult"/>
    /// containing one or more <see cref="EvaluationMetric"/>s.
    /// </summary>
    /// <param name="scenarioRun">The <see cref="ScenarioRun"/> of which this evaluation is a part.</param>
    /// <param name="modelResponse">The response that is to be evaluated.</param>
    /// <param name="additionalContext">
    /// Additional contextual information that the <see cref="IEvaluator"/>s included in this <see cref="ScenarioRun"/>
    /// may need to accurately evaluate the supplied <paramref name="modelResponse"/>.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can cancel the evaluation operation.
    /// </param>
    /// <returns>An <see cref="EvaluationResult"/> containing one or more <see cref="EvaluationMetric"/>s.</returns>
    public static ValueTask<EvaluationResult> EvaluateAsync(
        this ScenarioRun scenarioRun,
        ChatMessage modelResponse,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(scenarioRun, nameof(scenarioRun));

        return scenarioRun.EvaluateAsync(
                messages: [],
                modelResponse,
                additionalContext,
                cancellationToken);
    }

    /// <summary>
    /// Evaluates the supplied <paramref name="modelResponse"/> and returns an <see cref="EvaluationResult"/>
    /// containing one or more <see cref="EvaluationMetric"/>s.
    /// </summary>
    /// <param name="scenarioRun">The <see cref="ScenarioRun"/> of which this evaluation is a part.</param>
    /// <param name="userRequest">
    /// The request that produced the <paramref name="modelResponse"/> that is to be evaluated.
    /// </param>
    /// <param name="modelResponse">The response that is to be evaluated.</param>
    /// <param name="additionalContext">
    /// Additional contextual information (beyond that which is available in <paramref name="userRequest"/>) that the
    /// <see cref="IEvaluator"/>s included in this <see cref="ScenarioRun"/> may need to accurately evaluate the
    /// supplied <paramref name="modelResponse"/>.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can cancel the evaluation operation.
    /// </param>
    /// <returns>An <see cref="EvaluationResult"/> containing one or more <see cref="EvaluationMetric"/>s.</returns>
    public static ValueTask<EvaluationResult> EvaluateAsync(
        this ScenarioRun scenarioRun,
        ChatMessage userRequest,
        ChatMessage modelResponse,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(scenarioRun, nameof(scenarioRun));

        return scenarioRun.EvaluateAsync(
                messages: [userRequest],
                modelResponse,
                additionalContext,
                cancellationToken);
    }
}
