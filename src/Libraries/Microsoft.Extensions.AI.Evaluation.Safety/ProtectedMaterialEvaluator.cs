// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

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
public sealed class ProtectedMaterialEvaluator()
    : ContentSafetyEvaluator(
        contentSafetyServiceAnnotationTask: "protected material",
        metricNames:
            new Dictionary<string, string>
            {
                ["protected_material"] = ProtectedMaterialMetricName,
                ["artwork"] = ProtectedArtworkMetricName,
                ["fictional_characters"] = ProtectedFictionalCharactersMetricName,
                ["logos_and_brands"] = ProtectedLogosAndBrandsMetricName
            })
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
    public override async ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(chatConfiguration);
        _ = Throw.IfNull(modelResponse);

        IChatClient chatClient = chatConfiguration.ChatClient;

        // First evaluate the text content in the conversation for protected material.
        EvaluationResult result =
            await EvaluateContentSafetyAsync(
                chatClient,
                messages,
                modelResponse,
                contentSafetyServicePayloadFormat: ContentSafetyServicePayloadFormat.HumanSystem.ToString(),
                includeMetricNamesInContentSafetyServicePayload: false,
                cancellationToken: cancellationToken).ConfigureAwait(false);

        // If images are present in the conversation, do a second evaluation for protected material in images.
        // The content safety service does not support evaluating both text and images in the same request currently.
        if (messages.ContainsImageWithSupportedFormat() || modelResponse.ContainsImageWithSupportedFormat())
        {
            EvaluationResult imageResult =
                await EvaluateContentSafetyAsync(
                    chatClient,
                    messages,
                    modelResponse,
                    contentSafetyServicePayloadFormat: ContentSafetyServicePayloadFormat.Conversation.ToString(),
                    includeMetricNamesInContentSafetyServicePayload: false,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

            foreach (EvaluationMetric imageMetric in imageResult.Metrics.Values)
            {
                result.Metrics[imageMetric.Name] = imageMetric;
            }
        }

        return result;
    }
}
