// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.Quality;

/// <summary>
/// An <see cref="IEvaluator"/> that evaluates the 'Groundedness' of a response produced by an AI model.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="GroundednessEvaluator"/> measures the degree to which the response being evaluated is grounded in
/// the information present in the supplied <see cref="GroundednessEvaluatorContext.GroundingContext"/>. It returns a
/// <see cref="NumericMetric"/> that contains a score for the 'Groundedness'. The score is a number between 1 and 5,
/// with 1 indicating a poor score, and 5 indicating an excellent score.
/// </para>
/// <para>
/// <b>Note:</b> <see cref="GroundednessEvaluator"/> is an AI-based evaluator that uses an AI model to perform its
/// evaluation. While the prompt that this evaluator uses to perform its evaluation is designed to be model-agnostic,
/// the performance of this prompt (and the resulting evaluation) can vary depending on the model used, and can be
/// especially poor when a smaller / local model is used.
/// </para>
/// <para>
/// The prompt that <see cref="GroundednessEvaluator"/> uses has been tested against (and tuned to work well with) the
/// following models. So, using this evaluator with a model from the following list is likely to produce the best
/// results. (The model to be used can be configured via <see cref="ChatConfiguration.ChatClient"/>.)
/// </para>
/// <para>
/// <b>GPT-4o</b>
/// </para>
/// </remarks>
public sealed class GroundednessEvaluator : SingleNumericMetricEvaluator
{
    /// <summary>
    /// Gets the <see cref="EvaluationMetric.Name"/> of the <see cref="NumericMetric"/> returned by
    /// <see cref="GroundednessEvaluator"/>.
    /// </summary>
    public static string GroundednessMetricName => "Groundedness";

    /// <inheritdoc/>
    protected override string MetricName => GroundednessMetricName;

    /// <inheritdoc/>
    protected override bool IgnoresHistory => false;

    /// <inheritdoc/>
    public override async ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        EvaluationResult result =
            await base.EvaluateAsync(
                messages,
                modelResponse,
                chatConfiguration,
                additionalContext,
                cancellationToken).ConfigureAwait(false);

        if (GetRelevantContext(additionalContext) is GroundednessEvaluatorContext context)
        {
            result.AddOrUpdateContextInAllMetrics("Grounding Context", context.GetContents());
        }

        return result;
    }

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

        var builder = new StringBuilder();

        if (GetRelevantContext(additionalContext) is GroundednessEvaluatorContext context)
        {
            _ = builder.Append(context.GroundingContext);
            _ = builder.AppendLine();
            _ = builder.AppendLine();
        }

        if (conversationHistory is not null)
        {
            foreach (ChatMessage message in conversationHistory)
            {
                _ = builder.Append(await RenderAsync(message, cancellationToken).ConfigureAwait(false));
            }
        }

        string renderedContext = builder.ToString();

        string prompt =
            $$"""
            You will be presented with a QUESTION, and an ANSWER to the QUESTION along with some CONTEXT (which may
            include some conversation history). Groundedness of the ANSWER is measured by how well it logically follows
            from the information supplied via the CONTEXT and / or QUESTION.

            Score the groundedness of the ANSWER between one to five stars using the following rating scale:
            One star: the ANSWER is not at all grounded and is logically false based on the supplied info.
            Two stars: most parts of the ANSWER are not grounded and do not follow logically from the supplied info.
            Three stars: some parts of the ANSWER are grounded in the supplied info, other parts are not.
            Four stars: most parts of the ANSWER are grounded and follow logically from the supplied info.
            Five stars: the ANSWER is perfectly grounded and follows logically from the supplied info.

            If it is not possible to determine whether the ANSWER is logically true or false based on the supplied
            info, score the ANSWER as one star.

            Read the supplied QUESTION, ANSWER and CONTEXT thoroughly and select the correct rating based on the above
            criteria. Read the CONTEXT thoroughly to ensure you know what the CONTEXT entails. (Note that the ANSWER is
            generated by a computer system and can contain certain symbols. This should not be a negative factor in the
            evaluation.)

            The rating value should always be an integer between 1 and 5. So the rating produced should be 1 or 2 or 3
            or 4 or 5.

            Independent Examples:
            ## Example Task #1 Input:
            -----
            CONTEXT: Some are reported as not having been wanted at all.
            -----
            QUESTION:
            -----
            ANSWER: All are reported as being completely and fully wanted.
            -----
            ## Example Task #1 Output:
            1

            ## Example Task #2 Input:
            -----
            CONTEXT: Ten new television shows appeared during the month of September. Five of the shows were sitcoms,
            three were hourlong dramas, and two were news-magazine shows. By January, only seven of these new shows
            were still on the air. Five of the shows that remained were sitcoms.
            -----
            QUESTION: Were there any hourlong shows amongst the shows that were cancelled?,
            -----
            ANSWER: At least one of the shows that were cancelled was an hourlong drama.
            -----
            ## Example Task #2 Output:
            5

            ## Example Task #3 Input:
            -----
            CONTEXT: In Quebec, an allophone is a resident, usually an immigrant, whose mother tongue or home language
            is neither French nor English.
            -----
            QUESTION: What does the term allophone mean?
            -----
            ANSWER: In Quebec, an allophone is a resident, usually an immigrant, whose mother tongue or home language
            is not French.
            -----
            ## Example Task #3 Output:
            5

            ## Actual Task Input:
            -----
            CONTEXT: {{renderedContext}}
            -----
            QUESTION: {{renderedUserRequest}}
            -----
            ANSWER: {{renderedModelResponse}}
            -----

            ## Actual Task Output:
            """;

        return prompt;
    }

    private static GroundednessEvaluatorContext? GetRelevantContext(IEnumerable<EvaluationContext>? additionalContext)
    {
        if (additionalContext?.OfType<GroundednessEvaluatorContext>().FirstOrDefault()
                is GroundednessEvaluatorContext context)
        {
            return context;
        }

        return null;
    }
}
