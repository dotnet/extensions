// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.Evaluation.Quality.Utilities;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.Quality;

/// <summary>
/// An <see cref="IEvaluator"/> that evaluates the 'Relevance', 'Truth' and 'Completeness' of a response produced by an
/// AI model.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="RelevanceTruthAndCompletenessEvaluator"/> returns three <see cref="NumericMetric"/>s that contain scores
/// for 'Relevance', 'Truth' and 'Completeness' respectively. Each score is a number between 1 and 5, with 1 indicating
/// a poor score, and 5 indicating an excellent score. Each returned score is also accompanied by a
/// <see cref="EvaluationMetric.Reason"/> that provides an explanation for the score.
/// </para>
/// <para>
/// <b>Note:</b> <see cref="RelevanceTruthAndCompletenessEvaluator"/> is an AI-based evaluator that uses an AI model to
/// perform its evaluation. While the prompt that this evaluator uses to perform its evaluation is designed to be
/// model-agnostic, the performance of this prompt (and the resulting evaluation) can vary depending on the model used,
/// and can be especially poor when a smaller / local model is used.
/// </para>
/// <para>
/// The prompt that <see cref="RelevanceTruthAndCompletenessEvaluator"/> uses has been tested against (and tuned to
/// work well with) the following models. So, using this evaluator with a model from the following list is likely to
/// produce the best results. (The model to be used can be configured via <see cref="ChatConfiguration.ChatClient"/>.)
/// </para>
/// <para>
/// <b>GPT-4o</b>
/// </para>
/// </remarks>
/// <related type="Article" href="https://learn.microsoft.com/dotnet/ai/tutorials/evaluate-with-reporting">Tutorial: Evaluate a model's response with response caching and reporting.</related>
public sealed partial class RelevanceTruthAndCompletenessEvaluator : ChatConversationEvaluator
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
        if (conversationHistory is not null)
        {
            foreach (ChatMessage message in conversationHistory)
            {
                _ = builder.Append(await RenderAsync(message, cancellationToken).ConfigureAwait(false));
            }
        }

        string renderedHistory = builder.ToString();

        string prompt = Prompts.BuildEvaluationPrompt(renderedUserRequest, renderedModelResponse, renderedHistory);
        return prompt;
    }

    /// <inheritdoc/>
    protected override async ValueTask PerformEvaluationAsync(
        ChatConfiguration chatConfiguration,
        IList<ChatMessage> evaluationMessages,
        EvaluationResult result,
        CancellationToken cancellationToken)
    {
        ChatResponse evaluationResponse;
        Rating rating;
        string duration;
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            evaluationResponse =
                await chatConfiguration.ChatClient.GetResponseAsync(
                    evaluationMessages,
                    _chatOptions,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

            string evaluationResponseText = evaluationResponse.Text.Trim();
            if (string.IsNullOrEmpty(evaluationResponseText))
            {
                rating = Rating.Inconclusive;
                result.AddDiagnosticsToAllMetrics(
                    EvaluationDiagnostic.Error(
                        "Evaluation failed because the model failed to produce a valid evaluation response."));
            }
            else
            {
                try
                {
                    rating = Rating.FromJson(evaluationResponseText!);
                }
                catch (JsonException)
                {
                    try
                    {
                        string repairedJson =
                            await JsonOutputFixer.RepairJsonAsync(
                                chatConfiguration,
                                evaluationResponseText!,
                                cancellationToken).ConfigureAwait(false);

                        if (string.IsNullOrEmpty(repairedJson))
                        {
                            rating = Rating.Inconclusive;
                            result.AddDiagnosticsToAllMetrics(
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
                        result.AddDiagnosticsToAllMetrics(
                            EvaluationDiagnostic.Error(
                                $"""
                                Failed to repair the following response from the model and parse scores for '{RelevanceMetricName}', '{TruthMetricName}' and '{CompletenessMetricName}'.:
                                {evaluationResponseText}
                                {ex}
                                """));
                    }
                }
            }
        }
        finally
        {
            stopwatch.Stop();
            duration = $"{stopwatch.Elapsed.TotalSeconds.ToString("F2", CultureInfo.InvariantCulture)} s";
        }

        UpdateResult();

        void UpdateResult()
        {
            const string Rationales = "Rationales";
            const string Separator = "; ";

            var commonMetadata = new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(evaluationResponse.ModelId))
            {
                commonMetadata["evaluation-model-used"] = evaluationResponse.ModelId!;
            }

            if (evaluationResponse.Usage is UsageDetails usage)
            {
                if (usage.InputTokenCount is not null)
                {
                    commonMetadata["evaluation-input-tokens-used"] = $"{usage.InputTokenCount}";
                }

                if (usage.OutputTokenCount is not null)
                {
                    commonMetadata["evaluation-output-tokens-used"] = $"{usage.OutputTokenCount}";
                }

                if (usage.TotalTokenCount is not null)
                {
                    commonMetadata["evaluation-total-tokens-used"] = $"{usage.TotalTokenCount}";
                }
            }

            commonMetadata["evaluation-duration"] = duration;

            NumericMetric relevance = result.Get<NumericMetric>(RelevanceMetricName);
            relevance.Value = rating.Relevance;
            relevance.Interpretation = relevance.InterpretScore();
            if (!string.IsNullOrWhiteSpace(rating.RelevanceReasoning))
            {
                relevance.Reason = rating.RelevanceReasoning!;
            }

            relevance.AddOrUpdateMetadata(commonMetadata);
            if (rating.RelevanceReasons.Any())
            {
                string value = string.Join(Separator, rating.RelevanceReasons);
                relevance.AddOrUpdateMetadata(name: Rationales, value);
            }

            NumericMetric truth = result.Get<NumericMetric>(TruthMetricName);
            truth.Value = rating.Truth;
            truth.Interpretation = truth.InterpretScore();
            if (!string.IsNullOrWhiteSpace(rating.TruthReasoning))
            {
                truth.Reason = rating.TruthReasoning!;
            }

            truth.AddOrUpdateMetadata(commonMetadata);
            if (rating.TruthReasons.Any())
            {
                string value = string.Join(Separator, rating.TruthReasons);
                truth.AddOrUpdateMetadata(name: Rationales, value);
            }

            NumericMetric completeness = result.Get<NumericMetric>(CompletenessMetricName);
            completeness.Value = rating.Completeness;
            completeness.Interpretation = completeness.InterpretScore();
            if (!string.IsNullOrWhiteSpace(rating.CompletenessReasoning))
            {
                completeness.Reason = rating.CompletenessReasoning!;
            }

            completeness.AddOrUpdateMetadata(commonMetadata);
            if (rating.CompletenessReasons.Any())
            {
                string value = string.Join(Separator, rating.CompletenessReasons);
                completeness.AddOrUpdateMetadata(name: Rationales, value);
            }

            if (!string.IsNullOrWhiteSpace(rating.Error))
            {
                result.AddDiagnosticsToAllMetrics(EvaluationDiagnostic.Error(rating.Error!));
            }
        }
    }
}
