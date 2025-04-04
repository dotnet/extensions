// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI.Evaluation.Safety;

/// <summary>
/// An <see cref="IEvaluator"/> that utilizes the Azure AI Content Safety service to evaluate responses produced by an
/// AI model for presence of protected material.
/// </summary>
/// <remarks>
/// <para>
/// Protected material includes any text that is under copyright, including song lyrics, recipes, and articles. Note
/// that <see cref="ProtectedMaterialEvaluator"/> can also detect protected material present within image content in
/// the evaluated responses. Supported file formats include JPG/JPEG, PNG and GIF and the evaluation can detect
/// copyrighted artwork, fictional characters, and logos and branding that are registered trademarks. Other modalities
/// such as audio and video are currently not supported.
/// </para>
/// <para>
/// <see cref="ProtectedMaterialEvaluator"/> returns a <see cref="BooleanMetric"/> with a value of
/// <see langword="true"/> indicating the presence of protected material in the response, and a value of
/// <see langword="false"/> indicating the absence of protected material.
/// </para>
/// </remarks>
/// <param name="contentSafetyServiceConfiguration">
/// Specifies the Azure AI project that should be used and credentials that should be used when this
/// <see cref="ContentSafetyEvaluator"/> communicates with the Azure AI Content Safety service to perform evaluations.
/// </param>
public sealed class ProtectedMaterialEvaluator(ContentSafetyServiceConfiguration contentSafetyServiceConfiguration)
    : ContentSafetyEvaluator(
        contentSafetyServiceConfiguration,
        contentSafetyServiceAnnotationTask: "protected material",
        evaluatorName: nameof(ProtectedMaterialEvaluator))
{
    /// <summary>
    /// Gets the <see cref="EvaluationMetric.Name"/> of the <see cref="BooleanMetric"/> returned by
    /// <see cref="ProtectedMaterialEvaluator"/> for indicating presence of protected material in responses.
    /// </summary>
    public static string ProtectedMaterialMetricName => "Protected Material";

    /// <summary>
    /// Gets the <see cref="EvaluationMetric.Name"/> of the <see cref="BooleanMetric"/> returned by
    /// <see cref="ProtectedMaterialEvaluator"/> for indicating presence of protected material in artwork in images.
    /// </summary>
    public static string ProtectedArtworkMetricName => "Protected Artwork";

    /// <summary>
    /// Gets the <see cref="EvaluationMetric.Name"/> of the <see cref="BooleanMetric"/> returned by
    /// <see cref="ProtectedMaterialEvaluator"/> for indicating presence of protected fictional characters in images.
    /// </summary>
    public static string ProtectedFictionalCharactersMetricName => "Protected Fictional Characters";

    /// <summary>
    /// Gets the <see cref="EvaluationMetric.Name"/> of the <see cref="BooleanMetric"/> returned by
    /// <see cref="ProtectedMaterialEvaluator"/> for indicating presence of protected logos and brands in images.
    /// </summary>
    public static string ProtectedLogosAndBrandsMetricName => "Protected Logos And Brands";

    /// <inheritdoc/>
    public override IReadOnlyCollection<string> EvaluationMetricNames =>
        [
            ProtectedMaterialMetricName,
            ProtectedArtworkMetricName,
            ProtectedFictionalCharactersMetricName,
            ProtectedLogosAndBrandsMetricName
        ];

    /// <inheritdoc/>
    public override async ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        // First evaluate the text content in the conversation for protected material.
        EvaluationResult result =
            await EvaluateContentSafetyAsync(
                messages,
                modelResponse,
                contentSafetyServicePayloadFormat: ContentSafetyServicePayloadFormat.HumanSystem.ToString(),
                cancellationToken: cancellationToken).ConfigureAwait(false);

        // If images are present in the conversation, do a second evaluation for protected material in images.
        // The content safety service does not support evaluating both text and images in the same request currently.
        if (messages.ContainImage() || modelResponse.ContainsImage())
        {
            EvaluationResult imageResult =
                await EvaluateContentSafetyAsync(
                    messages,
                    modelResponse,
                    contentSafetyServicePayloadFormat: ContentSafetyServicePayloadFormat.Conversation.ToString(),
                    cancellationToken: cancellationToken).ConfigureAwait(false);

            foreach (EvaluationMetric imageMetric in imageResult.Metrics.Values)
            {
                result.Metrics[imageMetric.Name] = imageMetric;
            }
        }

        IEnumerable<EvaluationMetric> updatedMetrics =
            result.Metrics.Values.Select(
                metric =>
                {
                    switch (metric.Name)
                    {
                        case "protected_material":
                            metric.Name = ProtectedMaterialMetricName;
                            return metric;
                        case "artwork":
                            metric.Name = ProtectedArtworkMetricName;
                            return metric;
                        case "fictional_characters":
                            metric.Name = ProtectedFictionalCharactersMetricName;
                            return metric;
                        case "logos_and_brands":
                            metric.Name = ProtectedLogosAndBrandsMetricName;
                            return metric;
                        default:
                            return metric;
                    }
                });

        result = new EvaluationResult(updatedMetrics);
        result.Interpret(metric => metric is BooleanMetric booleanMetric ? booleanMetric.InterpretScore() : null);
        return result;
    }
}
