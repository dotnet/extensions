// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI.Evaluation.Quality;

/// <summary>
/// An <see cref="IEvaluator"/> that evaluates the 'Coherence' of a response produced by an AI model.
/// </summary>
/// <remarks>
/// <see cref="CoherenceEvaluator"/> returns a <see cref="NumericMetric"/> that contains a score for 'Coherence'. The
/// score is a number between 1 and 5, with 1 indicating a poor score, and 5 indicating an excellent score.
/// </remarks>
public sealed class CoherenceEvaluator : SingleNumericMetricEvaluator
{
    /// <summary>
    /// Gets the <see cref="EvaluationMetric.Name"/> of the <see cref="NumericMetric"/> returned by
    /// <see cref="CoherenceEvaluator"/>.
    /// </summary>
    public static string CoherenceMetricName => "Coherence";

    /// <inheritdoc/>
    protected override string MetricName => CoherenceMetricName;

    /// <inheritdoc/>
    protected override bool IgnoresHistory => true;

    /// <inheritdoc/>
    protected override async ValueTask<string> RenderEvaluationPromptAsync(
        ChatMessage? userRequest,
        ChatMessage modelResponse,
        IEnumerable<ChatMessage>? includedHistory,
        IEnumerable<EvaluationContext>? additionalContext,
        CancellationToken cancellationToken)
    {
        string renderedModelResponse = await RenderAsync(modelResponse, cancellationToken).ConfigureAwait(false);

        string renderedUserRequest =
            userRequest is not null
                ? await RenderAsync(userRequest, cancellationToken).ConfigureAwait(false)
                : string.Empty;

        string prompt =
            $$"""
            Coherence of an answer is measured by how well all the sentences fit together and sound naturally as a
            whole. Consider the overall quality of the answer when evaluating coherence.

            Given the question and answer, score the coherence of the answer between one to five stars using the
            following rating scale:
            One star: the answer completely lacks coherence
            Two stars: the answer mostly lacks coherence
            Three stars: the answer is partially coherent
            Four stars: the answer is mostly coherent
            Five stars: the answer has perfect coherency

            The rating value should always be an integer between 1 and 5. So the rating produced should be 1 or 2 or 3
            or 4 or 5.

            question: What is your favorite indoor activity and why do you enjoy it?
            answer: I like pizza. The sun is shining.
            stars: 1

            question: Can you describe your favorite movie without giving away any spoilers?
            answer: It is a science fiction movie. There are dinosaurs. The actors eat cake. People must stop the
            villain.
            stars: 2

            question: What are some benefits of regular exercise?
            answer: Regular exercise improves your mood. A good workout also helps you sleep better. Trees are green.
            stars: 3

            question: How do you cope with stress in your daily life?
            answer: I usually go for a walk to clear my head. Listening to music helps me relax as well. Stress is a
            part of life, but we can manage it through some activities.
            stars: 4

            question: What can you tell me about climate change and its effects on the environment?
            answer: Climate change has far-reaching effects on the environment. Rising temperatures result in the
            melting of polar ice caps, contributing to sea-level rise. Additionally, more frequent and severe weather
            events, such as hurricanes and heatwaves, can cause disruption to ecosystems and human societies alike.
            stars: 5

            question: {{renderedUserRequest}}
            answer: {{renderedModelResponse}}
            stars:
            """;

        return prompt;
    }
}
