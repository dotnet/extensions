// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI.Evaluation.Quality;

/// <summary>
/// An <see cref="IEvaluator"/> that evaluates the 'Equivalence' of a response produced by an AI model.
/// </summary>
/// <remarks>
/// The <see cref="EquivalenceEvaluator"/> measures the degree to which the response being evaluated is similar to the
/// response supplied via <see cref="EquivalenceEvaluatorContext.GroundTruth"/>. It returns a
/// <see cref="NumericMetric"/> that contains a score for the 'Equivalence'. The score is a number between 1 and 5,
/// with 1 indicating a poor score, and 5 indicating an excellent score.
/// </remarks>
public sealed class EquivalenceEvaluator : SingleNumericMetricEvaluator
{
    /// <summary>
    /// Gets the <see cref="EvaluationMetric.Name"/> of the <see cref="NumericMetric"/> returned by
    /// <see cref="EquivalenceEvaluator"/>.
    /// </summary>
    public static string EquivalenceMetricName => "Equivalence";

    /// <inheritdoc/>
    protected override string MetricName => EquivalenceMetricName;

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

        string groundTruth;

        if (additionalContext?.OfType<EquivalenceEvaluatorContext>().FirstOrDefault()
                is EquivalenceEvaluatorContext context)
        {
            groundTruth = context.GroundTruth;
        }
        else
        {
            throw new InvalidOperationException(
                $"A value of type '{nameof(EquivalenceEvaluatorContext)}' was not found in the '{nameof(additionalContext)}' collection.");
        }

        string prompt =
            $$"""
            Equivalence, as a metric, measures the similarity between the predicted answer and the correct answer. If
            the information and content in the predicted answer is similar or equivalent to the correct answer, then
            the value of the Equivalence metric should be high, else it should be low.

            Given the question, correct answer, and predicted answer, determine the value of Equivalence metric using
            the following rating scale:
            One star: the predicted answer is not at all similar to the correct answer
            Two stars: the predicted answer is mostly not similar to the correct answer
            Three stars: the predicted answer is somewhat similar to the correct answer
            Four stars: the predicted answer is mostly similar to the correct answer
            Five stars: the predicted answer is completely similar to the correct answer

            The rating value should always be an integer between 1 and 5. So the rating produced should be 1 or 2 or 3
            or 4 or 5.

            The examples below show the Equivalence score for a question, a correct answer, and a predicted answer.

            question: What is the role of ribosomes?
            correct answer: Ribosomes are cellular structures responsible for protein synthesis. They interpret the
            genetic information carried by messenger RNA (mRNA) and use it to assemble amino acids into proteins.
            predicted answer: Ribosomes participate in carbohydrate breakdown by removing nutrients from complex sugar
            molecules.
            stars: 1

            question: Why did the Titanic sink?
            correct answer: The Titanic sank after it struck an iceberg during its maiden voyage in 1912. The impact
            caused the ship's hull to breach, allowing water to flood into the vessel. The ship's design, lifeboat
            shortage, and lack of timely rescue efforts contributed to the tragic loss of life.
            predicted answer: The sinking of the Titanic was a result of a large iceberg collision. This caused the
            ship to take on water and eventually sink, leading to the death of many passengers due to a shortage of
            lifeboats and insufficient rescue attempts.
            stars: 2

            question: What causes seasons on Earth?
            correct answer: Seasons on Earth are caused by the tilt of the Earth's axis and its revolution around the
            Sun. As the Earth orbits the Sun, the tilt causes different parts of the planet to receive varying amounts
            of sunlight, resulting in changes in temperature and weather patterns.
            predicted answer: Seasons occur because of the Earth's rotation and its elliptical orbit around the Sun.
            The tilt of the Earth's axis causes regions to be subjected to different sunlight intensities, which leads
            to temperature fluctuations and alternating weather conditions.
            stars: 3

            question: How does photosynthesis work?
            correct answer: Photosynthesis is a process by which green plants and some other organisms convert light
            energy into chemical energy. This occurs as light is absorbed by chlorophyll molecules, and then carbon
            dioxide and water are converted into glucose and oxygen through a series of reactions.
            predicted answer: In photosynthesis, sunlight is transformed into nutrients by plants and certain
            microorganisms. Light is captured by chlorophyll molecules, followed by the conversion of carbon dioxide
            and water into sugar and oxygen through multiple reactions.
            stars: 4

            question: What are the health benefits of regular exercise?
            correct answer: Regular exercise can help maintain a healthy weight, increase muscle and bone strength, and
            reduce the risk of chronic diseases. It also promotes mental well-being by reducing stress and improving
            overall mood.
            predicted answer: Routine physical activity can contribute to maintaining ideal body weight, enhancing
            muscle and bone strength, and preventing chronic illnesses. In addition, it supports mental health by
            alleviating stress and augmenting general mood.
            stars: 5

            question: {{renderedUserRequest}}
            correct answer:{{groundTruth}}
            predicted answer: {{renderedModelResponse}}
            stars:
            """;

        return prompt;
    }
}
