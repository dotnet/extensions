// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation;

/// <summary>
/// Extension methods for <see cref="IEvaluator"/>.
/// </summary>
public static class EvaluatorExtensions
{
    /// <summary>
    /// Evaluates the supplied <paramref name="modelResponse"/> and returns an <see cref="EvaluationResult"/>
    /// containing one or more <see cref="EvaluationMetric"/>s.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="EvaluationMetric.Name"/>s of the <see cref="EvaluationMetric"/>s contained in the returned
    /// <see cref="EvaluationResult"/> should match <see cref="IEvaluator.EvaluationMetricNames"/>.
    /// </para>
    /// <para>
    /// Also note that <paramref name="chatConfiguration"/> must not be omitted if the evaluation is performed using an
    /// AI model.
    /// </para>
    /// </remarks>
    /// <param name="evaluator">The <see cref="IEvaluator"/> that should perform the evaluation.</param>
    /// <param name="modelResponse">The response that is to be evaluated.</param>
    /// <param name="chatConfiguration">
    /// A <see cref="ChatConfiguration"/> that specifies the <see cref="IChatClient"/> and the
    /// <see cref="IEvaluationTokenCounter"/> that should be used if the evaluation is performed using an AI model.
    /// </param>
    /// <param name="additionalContext">
    /// Additional contextual information that the <paramref name="evaluator"/> may need to accurately evaluate the
    /// supplied <paramref name="modelResponse"/>.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can cancel the evaluation operation.
    /// </param>
    /// <returns>An <see cref="EvaluationResult"/> containing one or more <see cref="EvaluationMetric"/>s.</returns>
    public static ValueTask<EvaluationResult> EvaluateAsync(
        this IEvaluator evaluator,
        string modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default) =>
            evaluator.EvaluateAsync(
                modelResponse: new ChatMessage(ChatRole.Assistant, modelResponse),
                chatConfiguration,
                additionalContext: additionalContext,
                cancellationToken: cancellationToken);

    /// <summary>
    /// Evaluates the supplied <paramref name="modelResponse"/> and returns an <see cref="EvaluationResult"/>
    /// containing one or more <see cref="EvaluationMetric"/>s.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="EvaluationMetric.Name"/>s of the <see cref="EvaluationMetric"/>s contained in the returned
    /// <see cref="EvaluationResult"/> should match <see cref="IEvaluator.EvaluationMetricNames"/>.
    /// </para>
    /// <para>
    /// Also note that <paramref name="chatConfiguration"/> must not be omitted if the evaluation is performed using an
    /// AI model.
    /// </para>
    /// </remarks>
    /// <param name="evaluator">The <see cref="IEvaluator"/> that should perform the evaluation.</param>
    /// <param name="userRequest">
    /// The request that produced the <paramref name="modelResponse"/> that is to be evaluated.
    /// </param>
    /// <param name="modelResponse">The response that is to be evaluated.</param>
    /// <param name="chatConfiguration">
    /// A <see cref="ChatConfiguration"/> that specifies the <see cref="IChatClient"/> and the
    /// <see cref="IEvaluationTokenCounter"/> that should be used if the evaluation is performed using an AI model.
    /// </param>
    /// <param name="additionalContext">
    /// Additional contextual information (beyond that which is available in <paramref name="userRequest"/>) that the
    /// <paramref name="evaluator"/> may need to accurately evaluate the supplied <paramref name="modelResponse"/>.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can cancel the evaluation operation.
    /// </param>
    /// <returns>An <see cref="EvaluationResult"/> containing one or more <see cref="EvaluationMetric"/>s.</returns>
    public static ValueTask<EvaluationResult> EvaluateAsync(
        this IEvaluator evaluator,
        string userRequest,
        string modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default) =>
            evaluator.EvaluateAsync(
                userRequest: new ChatMessage(ChatRole.User, userRequest),
                modelResponse: new ChatMessage(ChatRole.Assistant, modelResponse),
                chatConfiguration,
                additionalContext: additionalContext,
                cancellationToken: cancellationToken);

    /// <summary>
    /// Evaluates the supplied <paramref name="modelResponse"/> and returns an <see cref="EvaluationResult"/>
    /// containing one or more <see cref="EvaluationMetric"/>s.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="EvaluationMetric.Name"/>s of the <see cref="EvaluationMetric"/>s contained in the returned
    /// <see cref="EvaluationResult"/> should match <see cref="IEvaluator.EvaluationMetricNames"/>.
    /// </para>
    /// <para>
    /// Also note that <paramref name="chatConfiguration"/> must not be omitted if the evaluation is performed using an
    /// AI model.
    /// </para>
    /// </remarks>
    /// <param name="evaluator">The <see cref="IEvaluator"/> that should perform the evaluation.</param>
    /// <param name="modelResponse">The response that is to be evaluated.</param>
    /// <param name="chatConfiguration">
    /// A <see cref="ChatConfiguration"/> that specifies the <see cref="IChatClient"/> and the
    /// <see cref="IEvaluationTokenCounter"/> that should be used if the evaluation is performed using an AI model.
    /// </param>
    /// <param name="additionalContext">
    /// Additional contextual information that the <paramref name="evaluator"/> may need to accurately evaluate the
    /// supplied <paramref name="modelResponse"/>.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can cancel the evaluation operation.
    /// </param>
    /// <returns>An <see cref="EvaluationResult"/> containing one or more <see cref="EvaluationMetric"/>s.</returns>
    public static ValueTask<EvaluationResult> EvaluateAsync(
        this IEvaluator evaluator,
        ChatMessage modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(evaluator, nameof(evaluator));

        return evaluator.EvaluateAsync(
                messages: [],
                modelResponse,
                chatConfiguration,
                additionalContext,
                cancellationToken);
    }

    /// <summary>
    /// Evaluates the supplied <paramref name="modelResponse"/> and returns an <see cref="EvaluationResult"/>
    /// containing one or more <see cref="EvaluationMetric"/>s.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="EvaluationMetric.Name"/>s of the <see cref="EvaluationMetric"/>s contained in the returned
    /// <see cref="EvaluationResult"/> should match <see cref="IEvaluator.EvaluationMetricNames"/>.
    /// </para>
    /// <para>
    /// Also note that <paramref name="chatConfiguration"/> must not be omitted if the evaluation is performed using an
    /// AI model.
    /// </para>
    /// </remarks>
    /// <param name="evaluator">The <see cref="IEvaluator"/> that should perform the evaluation.</param>
    /// <param name="userRequest">
    /// The request that produced the <paramref name="modelResponse"/> that is to be evaluated.
    /// </param>
    /// <param name="modelResponse">The response that is to be evaluated.</param>
    /// <param name="chatConfiguration">
    /// A <see cref="ChatConfiguration"/> that specifies the <see cref="IChatClient"/> and the
    /// <see cref="IEvaluationTokenCounter"/> that should be used if the evaluation is performed using an AI model.
    /// </param>
    /// <param name="additionalContext">
    /// Additional contextual information (beyond that which is available in <paramref name="userRequest"/>) that the
    /// <paramref name="evaluator"/> may need to accurately evaluate the supplied <paramref name="modelResponse"/>.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can cancel the evaluation operation.
    /// </param>
    /// <returns>An <see cref="EvaluationResult"/> containing one or more <see cref="EvaluationMetric"/>s.</returns>
    public static ValueTask<EvaluationResult> EvaluateAsync(
        this IEvaluator evaluator,
        ChatMessage userRequest,
        ChatMessage modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(evaluator, nameof(evaluator));

        return evaluator.EvaluateAsync(
                messages: [userRequest],
                modelResponse,
                chatConfiguration,
                additionalContext,
                cancellationToken);
    }
}
