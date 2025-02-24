// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.Evaluation.Quality.Utilities;

namespace Microsoft.Extensions.AI.Evaluation.Quality;

/// <summary>
/// An <see cref="IEvaluator"/> that evaluates the 'Relevance', 'Truth' and 'Completeness' of a response produced by an
/// AI model.
/// </summary>
/// <remarks>
/// <see cref="RelevanceTruthAndCompletenessEvaluator"/> returns three <see cref="NumericMetric"/>s that contain scores
/// for 'Relevance', 'Truth' and 'Completeness' respectively. Each score is a number between 1 and 5, with 1 indicating
/// a poor score, and 5 indicating an excellent score.
/// </remarks>
/// <param name="options">Options for <see cref="RelevanceTruthAndCompletenessEvaluator"/>.</param>
public sealed partial class RelevanceTruthAndCompletenessEvaluator(
    RelevanceTruthAndCompletenessEvaluatorOptions? options = null) : ChatConversationEvaluator
{
    /// <summary>
    /// Gets the <see cref="EvaluationMetric.Name"/> of the <see cref="NumericMetric"/> returned by
    /// <see cref="RelevanceTruthAndCompletenessEvaluator"/> for 'Relevance'.
    /// </summary>
    public static string RelevanceMetricName => "Relevance";

    /// <summary>
    /// Gets the <see cref="EvaluationMetric.Name"/> of the <see cref="NumericMetric"/> returned by
    /// <see cref="RelevanceTruthAndCompletenessEvaluator"/> for 'Truth'.
    /// </summary>
    public static string TruthMetricName => "Truth";

    /// <summary>
    /// Gets the <see cref="EvaluationMetric.Name"/> of the <see cref="NumericMetric"/> returned by
    /// <see cref="RelevanceTruthAndCompletenessEvaluator"/> for 'Completeness'.
    /// </summary>
    public static string CompletenessMetricName => "Completeness";

    /// <inheritdoc/>
    public override IReadOnlyCollection<string> EvaluationMetricNames { get; } =
        [RelevanceMetricName, TruthMetricName, CompletenessMetricName];

    /// <inheritdoc/>
    protected override bool IgnoresHistory => false;

    private readonly ChatOptions _chatOptions =
        new ChatOptions
        {
            Temperature = 0.0f,
            ResponseFormat = ChatResponseFormat.Json
        };

    private readonly RelevanceTruthAndCompletenessEvaluatorOptions _options =
        options ?? RelevanceTruthAndCompletenessEvaluatorOptions.Default;

    /// <inheritdoc/>
    protected override EvaluationResult InitializeResult()
    {
        var relevance = new NumericMetric(RelevanceMetricName);
        var truth = new NumericMetric(TruthMetricName);
        var completeness = new NumericMetric(CompletenessMetricName);
        return new EvaluationResult(relevance, truth, completeness);
    }

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

        var builder = new StringBuilder();
        if (includedHistory is not null)
        {
            foreach (ChatMessage message in includedHistory)
            {
                _ = builder.Append(await RenderAsync(message, cancellationToken).ConfigureAwait(false));
            }
        }

        string renderedHistory = builder.ToString();

        string prompt =
            _options.IncludeReasoning
                ? Prompts.BuildEvaluationPromptWithReasoning(
                    renderedUserRequest,
                    renderedModelResponse,
                    renderedHistory)
                : Prompts.BuildEvaluationPrompt(
                    renderedUserRequest,
                    renderedModelResponse,
                    renderedHistory);

        return prompt;
    }

    /// <inheritdoc/>
    protected override async ValueTask PerformEvaluationAsync(
        ChatConfiguration chatConfiguration,
        IList<ChatMessage> evaluationMessages,
        EvaluationResult result,
        CancellationToken cancellationToken)
    {
        ChatResponse<Rating> evaluationResponse =
            await chatConfiguration.ChatClient.GetResponseAsync<Rating>(
                evaluationMessages,
                _chatOptions,
                cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!evaluationResponse.TryGetResult(out Rating? rating))
        {
            string? evaluationResponseText = evaluationResponse.Message.Text?.Trim();

            if (string.IsNullOrWhiteSpace(evaluationResponseText))
            {
                rating = Rating.Inconclusive;
                result.AddDiagnosticToAllMetrics(
                    EvaluationDiagnostic.Error(
                        "Evaluation failed because the model failed to produce a valid evaluation response."));
            }
            else
            {
                try
                {
                    string? repairedJson =
                        await JsonOutputFixer.RepairJsonAsync(
                            chatConfiguration,
                            evaluationResponseText!,
                            cancellationToken).ConfigureAwait(false);

                    if (string.IsNullOrWhiteSpace(repairedJson))
                    {
                        rating = Rating.Inconclusive;
                        result.AddDiagnosticToAllMetrics(
                            EvaluationDiagnostic.Error(
                                $"""
                                Failed to repair the following response from the model and parse scores for '{RelevanceMetricName}', '{TruthMetricName}' and '{CompletenessMetricName}'.:
                                {evaluationResponseText}
                                """));
                    }
                    else
                    {
                        rating = Rating.FromJson(repairedJson!);
                    }
                }
                catch (JsonException ex)
                {
                    rating = Rating.Inconclusive;
                    result.AddDiagnosticToAllMetrics(
                        EvaluationDiagnostic.Error(
                            $"""
                            Failed to repair the following response from the model and parse scores for '{RelevanceMetricName}', '{TruthMetricName}' and '{CompletenessMetricName}'.:
                            {evaluationResponseText}
                            {ex}
                            """));
                }
            }
        }

        UpdateResult(rating);

        void UpdateResult(Rating rating)
        {
            NumericMetric relevance = result.Get<NumericMetric>(RelevanceMetricName);
            relevance.Value = rating.Relevance;
            relevance.Interpretation = relevance.InterpretScore();
            if (!string.IsNullOrWhiteSpace(rating.RelevanceReasoning))
            {
                relevance.AddDiagnostic(EvaluationDiagnostic.Informational(rating.RelevanceReasoning!));
            }

            NumericMetric truth = result.Get<NumericMetric>(TruthMetricName);
            truth.Value = rating.Truth;
            truth.Interpretation = truth.InterpretScore();
            if (!string.IsNullOrWhiteSpace(rating.TruthReasoning))
            {
                truth.AddDiagnostic(EvaluationDiagnostic.Informational(rating.TruthReasoning!));
            }

            NumericMetric completeness = result.Get<NumericMetric>(CompletenessMetricName);
            completeness.Value = rating.Completeness;
            completeness.Interpretation = completeness.InterpretScore();
            if (!string.IsNullOrWhiteSpace(rating.CompletenessReasoning))
            {
                completeness.AddDiagnostic(EvaluationDiagnostic.Informational(rating.CompletenessReasoning!));
            }

            if (!string.IsNullOrWhiteSpace(rating.Error))
            {
                result.AddDiagnosticToAllMetrics(EvaluationDiagnostic.Error(rating.Error!));
            }
        }
    }
}
