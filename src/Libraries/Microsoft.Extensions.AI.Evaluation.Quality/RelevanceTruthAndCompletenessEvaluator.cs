// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.Evaluation.Quality.Utilities;
using Microsoft.Extensions.AI.Evaluation.Utilities;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.Quality;

/// <summary>
/// An <see cref="IEvaluator"/> that evaluates the 'Relevance', 'Truth' and 'Completeness' of a response produced by an
/// AI model.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="RelevanceTruthAndCompletenessEvaluator"/> returns three <see cref="NumericMetric"/>s that contain scores
/// for 'Relevance (RTC)', 'Truth (RTC)' and 'Completeness (RTC)' respectively. Each score is a number between 1 and 5,
/// with 1 indicating a poor score, and 5 indicating an excellent score.
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
/// <related type="Article" href="https://learn.microsoft.com/dotnet/ai/tutorials/evaluate-with-reporting">
/// Tutorial: Evaluate a model's response with response caching and reporting.
/// </related>
[Experimental("AIEVAL001")]
public sealed class RelevanceTruthAndCompletenessEvaluator : IEvaluator
{
    /// <summary>
    /// Gets the <see cref="EvaluationMetric.Name"/> of the <see cref="NumericMetric"/> returned by
    /// <see cref="RelevanceTruthAndCompletenessEvaluator"/> for 'Relevance'.
    /// </summary>
    public static string RelevanceMetricName => "Relevance (RTC)";

    /// <summary>
    /// Gets the <see cref="EvaluationMetric.Name"/> of the <see cref="NumericMetric"/> returned by
    /// <see cref="RelevanceTruthAndCompletenessEvaluator"/> for 'Truth'.
    /// </summary>
    public static string TruthMetricName => "Truth (RTC)";

    /// <summary>
    /// Gets the <see cref="EvaluationMetric.Name"/> of the <see cref="NumericMetric"/> returned by
    /// <see cref="RelevanceTruthAndCompletenessEvaluator"/> for 'Completeness'.
    /// </summary>
    public static string CompletenessMetricName => "Completeness (RTC)";

    /// <inheritdoc/>
    public IReadOnlyCollection<string> EvaluationMetricNames { get; } =
        [RelevanceMetricName, TruthMetricName, CompletenessMetricName];

    private readonly ChatOptions _chatOptions =
        new ChatOptions
        {
            Temperature = 0.0f,
            ResponseFormat = ChatResponseFormat.Json
        };

    /// <inheritdoc/>
    public async ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(modelResponse);
        _ = Throw.IfNull(chatConfiguration);

        var relevance = new NumericMetric(RelevanceMetricName);
        var truth = new NumericMetric(TruthMetricName);
        var completeness = new NumericMetric(CompletenessMetricName);
        var result = new EvaluationResult(relevance, truth, completeness);

        if (!messages.TryGetUserRequest(
                out ChatMessage? userRequest,
                out IReadOnlyList<ChatMessage> conversationHistory) ||
            string.IsNullOrWhiteSpace(userRequest.Text))
        {
            result.AddDiagnosticsToAllMetrics(
                EvaluationDiagnostic.Error(
                    $"The {nameof(messages)} supplied for evaluation did not contain a user request as the last message."));

            return result;
        }

        if (string.IsNullOrWhiteSpace(modelResponse.Text))
        {
            result.AddDiagnosticsToAllMetrics(
                EvaluationDiagnostic.Error($"The {nameof(modelResponse)} supplied for evaluation was null or empty."));

            return result;
        }

        List<ChatMessage> evaluationInstructions =
            GetEvaluationInstructions(userRequest, modelResponse, conversationHistory);

        (ChatResponse evaluationResponse, TimeSpan evaluationDuration) =
            await TimingHelper.ExecuteWithTimingAsync(() =>
                chatConfiguration.ChatClient.GetResponseAsync(
                    evaluationInstructions,
                    _chatOptions,
                    cancellationToken)).ConfigureAwait(false);

        await ParseEvaluationResponseAsync(
            result,
            evaluationResponse,
            evaluationDuration,
            chatConfiguration,
            cancellationToken).ConfigureAwait(false);

        return result;
    }

    private static List<ChatMessage> GetEvaluationInstructions(
        ChatMessage userRequest,
        ChatResponse modelResponse,
        IEnumerable<ChatMessage> conversationHistory)
    {
        string renderedUserRequest = userRequest.RenderText();
        string renderedModelResponse = modelResponse.RenderText();
        string renderedConversationHistory = conversationHistory.RenderText();

#pragma warning disable S103 // Lines should not be too long
        string evaluationPrompt =
            $$"""
            Read the History, User Query, and Model Response below and produce your response as a single JSON object.
            Do not include any other text in your response besides the JSON object. Make sure the response is a valid
            JSON object.

            The JSON object should have the following format. However, do not include any markdown tags in your
            response. Your response should start with an open curly brace and end with a closing curly brace for the
            JSON.
            ```
            {
                "relevance": 1,
                "relevanceReasoning": "The reason for the relevance score",
                "relevanceReasons": ["relevance_reason_distant_topic"],
                "truth": 1,
                "truthReasoning": "The reason for the truth score",
                "truthReasons": ["truth_reason_incorrect_information", "truth_reason_outdated_information", "truth_reason_misleading_incorrectforintent"],
                "completeness": 1,
                "completenessReasoning": "The reason for the completeness score",
                "completenessReasons": ["completeness_reason_no_solution", "completeness_reason_genericsolution_missingcode"],
            }
            ```

            -----

            History: {{renderedConversationHistory}}

            -----

            User Query: {{renderedUserRequest}}

            -----

            Model Response: {{renderedModelResponse}}

            -----

            That's the History, User Query, and Model Response you will rate. Now, in 3 Steps, you will evaluate the Model Response on 3 criteria.

            -----

            Step 1: Rate the relevance of the response.

            Regardless of truth of statements, how much of the response is directly related to the request?

            1 = None of the response is at all related
            2 = A little of the response is directly related, or the response is a little bit related
            3 = Some of the response is related, or the response is somewhat related
            4 = Most of the response is related, or the response is mostly related
            5 = 100% of the response is 100% related to the request

            Step 1a. Record your response as the value of the "relevance" property in the JSON output.

            Step 1b. Now, record your reasoning for that score as the value of the "relevanceReasoning" property. If the score is 5, record the value of "relevanceReasoning" as "100% of the response is 100% related to the request". If the score is not 5, write your Reasoning as fewer than 100 words and prioritizing the most important contributing reasons to the score.

            Step 1c. Now, record whether your relevance reasoning includes each of these categories, as an array of strings for the value of the "relevanceReasons" property. The possible values for this array are "relevance_reason_distant_topic", "relevance_reason_scope_too_small", "relevance_reason_scope_too_large", "relevance_reason_wrong_intent_served", or "relevance_reason_other". If the score is 5, the array should be empty.
            - relevance_reason_distant_topic. In a taxonomy of programming-related topics, the response is a non-zero distance from the user request. Example: User: "How can I implement a binary search in Python?" Model Response: "HTML (HyperText Markup Language) is the standard markup language for web pages."
            - relevance_reason_scope_too_small. Scope or level of abstraction is smaller than user intent. Example: User: "Can you explain object-oriented programming?" Model Response: "In object-oriented programming, a class is a blueprint for creating objects."
            - relevance_reason_scope_too_large. Scope or level of abstraction is larger than user intent.
            Example: User: "Why am I getting a null pointer exception here?" Model Response: "A Null Pointer Exception in Java occurs when you try to use a reference that points to no location in memory (null) as though it were referencing an object."
            - relevance_reason_wrong_intent_served. Alignment with inferred or explicit intent, or in absence of that, general standards. Example: User: "convert a JSON object to a Python dictionary" Model Response: "JSON is a lightweight data-interchange format that is easy for humans to read and write and easy for machines to parse and generate." (The user intends to write code and the model is serving an intent to explain a concept)
            - relevance_reason_other. Any other reasoning.

            -----

            Step 2: Rate the truth of the response.

            Read the History, Query, and Model Response again.

            Regardless of relevance, how true are the verifiable statements in the response?

            1 = The entire response is totally false
            2 = A little of the response is true, or the response is a little bit true
            3 = Some of the response is true, or the response is somewhat true
            4 = Most of the response is true, or the response is mostly true
            5 = 100% of the response is 100% true

            Step 2a. Record your response as the value of the "truth" property in the JSON output.

            Step 2b. Now, record your reasoning for that score as the value of the "truthReasoning" property. If the score is 5, record the value of "truthReasoning" as "100% of the response is 100% true". If the score is not 5, write your Reasoning as fewer than 100 words and prioritizing the most important contributing reasons to the score.

            Step 2c. Now, record whether your truth reasoning includes each of these categories, as an array of strings for the value of the "truthReasons" property. The possible values for this array are "truth_reason_incorrect_information", "truth_reason_outdated_information", "truth_reason_misleading_incorrectforintent", or "truth_reason_other". If the score is 5, the array should be empty.
            - truth_reason_incorrect_information. The response contains information that is factually incorrect. Example: User: "What is the time complexity of quicksort?" Model Response: "Quicksort has a time complexity of O(n)."
            - truth_reason_outdated_information. The response contains information that was once true but is no longer true. Example: User: "How do I install Python 2?" Model Response: "You can install Python 2 using the command sudo apt-get install python."
            - truth_reason_misleading_incorrectforintent. The response is true but irrelevant to the user's intent, causing results that are incorrect for the user's context. User: "How do I sort a list in Python?" Model Response: "You can use the sorted() function to sort a list in Python." (sorted() returns a new sorted list, leaving the original list unchanged. If the user's intent was to sort the original list, they should use list.sort().)
            - truth_reason_other. any other reasoning.

            -----

            Step 3: Rate the completeness of the response.

            Read the History, Query, and Model Response again.

            Regardless of whether the statements made in the response are true, how many of the points necessary to address the request, does the response contain?

            1 = The response omits all points that are necessary to address the request.
            2 = The response includes a little of the points that are necessary to address the request.
            3 = The response includes some of the points that are necessary to address the request.
            4 = The response includes most of the points that are necessary to address the request.
            5 = The response includes all points that are necessary to address the request. For explain tasks, nothing is left unexplained. For improve tasks, I looked for all potential improvements, and none were left out. For fix tasks, the response purports to get the user all the way to a fixed state (regardless of whether it actually works). For "do task" responses, it does everything requested.

            Step 3a. Record your response as the value of the "completeness" property in the JSON output.

            Step 3b. Now, record your reasoning for that score as the value of the "completenessReasoning" property. If the score is 5, record the value of "completenessReasoning" as "The response includes all points that are necessary to address the request". If the score is not 5, write your Reasoning as fewer than 100 words and prioritizing the most important contributing reasons to the score.

            Step 3c. Now, record whether your completeness reasoning includes each of these categories, as an array of strings for the value of the "completenessReasons" property. The possible values for this array are "completeness_reason_no_solution", "completeness_reason_lacks_information_about_solution", "completeness_reason_genericsolution_missingcode", "completeness_reason_generic_code", "completeness_reason_failed_to_change_code", "completeness_reason_failed_to_change_code", "completeness_reason_incomplete_list", "completeness_reason_incomplete_code", "completeness_reason_missing_warnings", or "completeness_reason_other". If the score is 5, the array should be empty.
            - completeness_reason_no_solution. The model response does not achieve or offer a solution to the user intent. Example 1: User: "How can I implement a binary search in Python?" Model Response: "Binary search is a search algorithm." Example 2: User: "How can I implement a binary search in Python?" Model Response: "500 error"
            - completeness_reason_lacks_information_about_solution. The model response does not include enough information about its solution, such as why its solution is reasonable, or how it addresses the user intent. Example: User: "How can I reverse a string in Python?" Model Response: "Hello, World!"[::-1]"
            - completeness_reason_genericsolution_missingcode. The user intends to generate code or get help writing code. The model response includes a response that solves the problem generically, but does not include code. Example: User: "How can I implement a binary search in Python?" Model Response: "You can implement a binary search by dividing the search space in half each time you fail to find the target value."
            - completeness_reason_generic_code. The user intends to generate code or get help writing code that uses specific functions, names, or other components in their current code. The model response includes generic code, and does not modify or use components from the user's current code. Example: User: "How do I use my foo function?" Model Response: "Here's how you can use a function in Python: function_name()."
            - completeness_reason_failed_to_change_code. The user intends to generate code or get help writing code, but the model response returns code that the user already has.
            - completeness_reason_incomplete_list. Serving the user intent requires several natural language components, such as a description of some concept, or a list of system capabilities, reasons to use a particular approach, or problems with code, but the model response addresses fewer than all of the required components or misses parts of components. Example: User: "What are the steps to implement a binary search in Python?" Model Response: "The first step in implementing a binary search is to sort the array."
            - completeness_reason_incomplete_code. Serving the user intent requires several code components, such as library imports, object creations and manipulations, and the model offers code, but the code offers fewer than all of the required components. Example: User: "How can I read a CSV file in Python?" Model response: "You can import the pandas library: `import pandas`."
            - completeness_reason_lazy_unopinionated. The model claims not to know how, or not be certain enough, to address the user intent and does not offer the user any recourse (e.g., asking the user to be more specific, or offering potential subtopics for ambiguous user requests). Example: User: "compile error" Model response: "I can't help with that, I need more information." (The response doesn't offer any typical troubleshooting ideas based on the user's code, context, or general programming knowledge.)
            - completeness_reason_missing_warnings. The response has potential pitfalls or dangers, but does not warn the user about them. Example: User: "How do I delete a file in Python?" Model Response: "You can use os.remove()." (This response should warn the user that this operation is irreversible and should be done with caution.)
            - completeness_reason_other. Any other reasoning.

            -----
            """;
#pragma warning restore S103

        return [new ChatMessage(ChatRole.User, evaluationPrompt)];
    }

    private static async ValueTask ParseEvaluationResponseAsync(
        EvaluationResult result,
        ChatResponse evaluationResponse,
        TimeSpan evaluationDuration,
        ChatConfiguration chatConfiguration,
        CancellationToken cancellationToken)
    {
        RelevanceTruthAndCompletenessRating rating;

        string evaluationResponseText = evaluationResponse.Text.Trim();
        if (string.IsNullOrEmpty(evaluationResponseText))
        {
            rating = RelevanceTruthAndCompletenessRating.Inconclusive;
            result.AddDiagnosticsToAllMetrics(
                EvaluationDiagnostic.Error("The model failed to produce a valid evaluation response."));
        }
        else
        {
            try
            {
                rating = RelevanceTruthAndCompletenessRating.FromJson(evaluationResponseText);
            }
            catch (JsonException)
            {
                try
                {
                    string repairedJson =
                        await JsonOutputFixer.RepairJsonAsync(
                            evaluationResponseText,
                            chatConfiguration,
                            cancellationToken).ConfigureAwait(false);

                    if (string.IsNullOrWhiteSpace(repairedJson))
                    {
                        rating = RelevanceTruthAndCompletenessRating.Inconclusive;
                        result.AddDiagnosticsToAllMetrics(
                            EvaluationDiagnostic.Error(
                                $"""
                                Failed to repair the following response from the model and parse scores for '{RelevanceMetricName}', '{TruthMetricName}' and '{CompletenessMetricName}':
                                {evaluationResponseText}
                                """));
                    }
                    else
                    {
                        rating = RelevanceTruthAndCompletenessRating.FromJson(repairedJson);
                    }
                }
                catch (JsonException ex)
                {
                    rating = RelevanceTruthAndCompletenessRating.Inconclusive;
                    result.AddDiagnosticsToAllMetrics(
                        EvaluationDiagnostic.Error(
                            $"""
                            Failed to repair the following response from the model and parse scores for '{RelevanceMetricName}', '{TruthMetricName}' and '{CompletenessMetricName}':
                            {evaluationResponseText}
                            {ex}
                            """));
                }
            }
        }

        UpdateResult();

        void UpdateResult()
        {
            const string Rationales = "Rationales";
            const string Separator = "; ";

            NumericMetric relevance = result.Get<NumericMetric>(RelevanceMetricName);
            relevance.AddOrUpdateChatMetadata(evaluationResponse, evaluationDuration);
            relevance.Value = rating.Relevance;
            relevance.Interpretation = relevance.InterpretScore();
            relevance.Reason = rating.RelevanceReasoning;

            if (rating.RelevanceReasons.Any())
            {
                string value = string.Join(Separator, rating.RelevanceReasons);
                relevance.AddOrUpdateMetadata(name: Rationales, value);
            }

            NumericMetric truth = result.Get<NumericMetric>(TruthMetricName);
            truth.AddOrUpdateChatMetadata(evaluationResponse, evaluationDuration);
            truth.Value = rating.Truth;
            truth.Interpretation = truth.InterpretScore();
            truth.Reason = rating.TruthReasoning;

            if (rating.TruthReasons.Any())
            {
                string value = string.Join(Separator, rating.TruthReasons);
                truth.AddOrUpdateMetadata(name: Rationales, value);
            }

            NumericMetric completeness = result.Get<NumericMetric>(CompletenessMetricName);
            completeness.AddOrUpdateChatMetadata(evaluationResponse, evaluationDuration);
            completeness.Value = rating.Completeness;
            completeness.Interpretation = completeness.InterpretScore();
            completeness.Reason = rating.CompletenessReasoning;

            if (rating.CompletenessReasons.Any())
            {
                string value = string.Join(Separator, rating.CompletenessReasons);
                completeness.AddOrUpdateMetadata(name: Rationales, value);
            }
        }
    }
}
