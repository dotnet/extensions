// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.Quality;

/// <summary>
/// An <see cref="IEvaluator"/> that evaluates the 'Fluency' of a response produced by an AI model.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="FluencyEvaluator"/> returns a <see cref="NumericMetric"/> that contains a score for 'Fluency'. The score
/// is a number between 1 and 5, with 1 indicating a poor score, and 5 indicating an excellent score.
/// </para>
/// <para>
/// <b>Note:</b> <see cref="FluencyEvaluator"/> is an AI-based evaluator that uses an AI model to perform its
/// evaluation. While the prompt that this evaluator uses to perform its evaluation is designed to be model-agnostic,
/// the performance of this prompt (and the resulting evaluation) can vary depending on the model used, and can be
/// especially poor when a smaller / local model is used.
/// </para>
/// <para>
/// The prompt that <see cref="FluencyEvaluator"/> uses has been tested against (and tuned to work well with) the
/// following models. So, using this evaluator with a model from the following list is likely to produce the best
/// results. (The model to be used can be configured via <see cref="ChatConfiguration.ChatClient"/>.)
/// </para>
/// <para>
/// <b>GPT-4o</b>
/// </para>
/// </remarks>
public sealed class FluencyEvaluator : SingleNumericMetricEvaluator
{
    /// <summary>
    /// Gets the <see cref="EvaluationMetric.Name"/> of the <see cref="NumericMetric"/> returned by
    /// <see cref="FluencyEvaluator"/>.
    /// </summary>
    public static string FluencyMetricName => "Fluency";

    /// <inheritdoc/>
    protected override string MetricName => FluencyMetricName;

    /// <inheritdoc/>
    protected override bool IgnoresHistory => true;

    /// <inheritdoc/>
    protected override async ValueTask<string> RenderEvaluationPromptAsync(
        ChatMessage? userRequest,
        ChatResponse modelResponse,
        IEnumerable<ChatMessage>? conversationHistory,
        IEnumerable<EvaluationContext>? additionalContext,
        CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(modelResponse);

        string renderedModelResponse = await RenderAsync(modelResponse, cancellationToken).ConfigureAwait(false);

        string renderedUserRequest =
            userRequest is not null
                ? await RenderAsync(userRequest, cancellationToken).ConfigureAwait(false)
                : string.Empty;

        string prompt =
            $$"""
            Fluency measures the quality of individual sentences in the answer, and whether they are well-written and
            grammatically correct. Consider the quality of individual sentences when evaluating fluency.

            Given the question and answer, score the fluency of the answer between one to five stars using the
            following rating scale:
            One star: the answer completely lacks fluency
            Two stars: the answer mostly lacks fluency
            Three stars: the answer is partially fluent
            Four stars: the answer is mostly fluent
            Five stars: the answer has perfect fluency

            The rating value should always be an integer between 1 and 5. So the rating produced should be 1 or 2 or 3
            or 4 or 5.

            question: What did you have for breakfast today?
            answer: Breakfast today, me eating cereal and orange juice very good.
            stars: 1

            question: How do you feel when you travel alone?
            answer: Alone travel, nervous, but excited also. I feel adventure and like its time.
            stars: 2

            question: When was the last time you went on a family vacation?
            answer: Last family vacation, it took place in last summer. We traveled to a beach destination, very fun.
            stars: 3

            question: What is your favorite thing about your job?
            answer: My favorite aspect of my job is the chance to interact with diverse people. I am constantly
            learning from their experiences and stories.
            stars: 4

            question: Can you describe your morning routine?
            answer: Every morning, I wake up at 6 am, drink a glass of water, and do some light stretching. After that,
            I take a shower and get dressed for work. Then, I have a healthy breakfast, usually consisting of oatmeal
            and fruits, before leaving the house around 7:30 am.
            stars: 5

            question: {{renderedUserRequest}}
            answer: {{renderedModelResponse}}
            stars:
            """;

        return prompt;
    }
}
