// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI.Evaluation;

/// <summary>
/// Evaluates responses produced by an AI model.
/// </summary>
public interface IEvaluator
{
    /// <summary>
    /// Gets the <see cref="EvaluationMetric.Name"/>s of the <see cref="EvaluationMetric"/>s produced by this
    /// <see cref="IEvaluator"/>.
    /// </summary>
    IReadOnlyCollection<string> EvaluationMetricNames { get; }

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
    /// Also note that <paramref name="chatConfiguration"/> must not be omitted if the evaluation is performed using an
    /// AI model.
    /// </para>
    /// </remarks>
    /// <param name="messages">
    /// The conversation history including the request that produced the supplied <paramref name="modelResponse"/>.
    /// </param>
    /// <param name="modelResponse">The response that is to be evaluated.</param>
    /// <param name="chatConfiguration">
    /// A <see cref="ChatConfiguration"/> that specifies the <see cref="IChatClient"/> and the
    /// <see cref="IEvaluationTokenCounter"/> that should be used if the evaluation is performed using an AI model.
    /// </param>
    /// <param name="additionalContext">
    /// Additional contextual information (beyond that which is available in <paramref name="messages"/>) that the
    /// <see cref="IEvaluator"/> may need to accurately evaluate the supplied <paramref name="modelResponse"/>.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can cancel the evaluation operation.
    /// </param>
    /// <returns>An <see cref="EvaluationResult"/> containing one or more <see cref="EvaluationMetric"/>s.</returns>
    ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatMessage modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default);
}
