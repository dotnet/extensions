// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.Evaluation.Utilities;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.Quality;

/// <summary>
/// An <see cref="IEvaluator"/> that evaluates the 'Relevance' of a response produced by an AI model.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="RelevanceEvaluator"/> measures an AI system's performance in understanding the input and generating
/// contextually appropriate responses. It returns a <see cref="NumericMetric"/> that contains a score for 'Relevance'.
/// The score is a number between 1 and 5, with 1 indicating a poor score, and 5 indicating an excellent score.
/// </para>
/// <para>
/// High relevance scores signify the AI system's understanding of the input and its capability to produce coherent
/// and contextually appropriate outputs. Conversely, low relevance scores indicate that generated responses might
/// be off-topic, lacking in context, or insufficient in addressing the user's intended queries.
/// </para>
/// <para>
/// <b>Note:</b> <see cref="RelevanceEvaluator"/> is an AI-based evaluator that uses an AI model to perform its
/// evaluation. While the prompt that this evaluator uses to perform its evaluation is designed to be model-agnostic,
/// the performance of this prompt (and the resulting evaluation) can vary depending on the model used, and can be
/// especially poor when a smaller / local model is used.
/// </para>
/// <para>
/// The prompt that <see cref="RelevanceEvaluator"/> uses has been tested against (and tuned to work well with) the
/// following models. So, using this evaluator with a model from the following list is likely to produce the best
/// results. (The model to be used can be configured via <see cref="ChatConfiguration.ChatClient"/>.)
/// </para>
/// <para>
/// <b>GPT-4o</b>
/// </para>
/// </remarks>
public sealed class RelevanceEvaluator : IEvaluator
{
    /// <summary>
    /// Gets the <see cref="EvaluationMetric.Name"/> of the <see cref="NumericMetric"/> returned by
    /// <see cref="RelevanceEvaluator"/>.
    /// </summary>
    public static string RelevanceMetricName => "Relevance";

    /// <inheritdoc/>
    public IReadOnlyCollection<string> EvaluationMetricNames { get; } = [RelevanceMetricName];

    private static readonly ChatOptions _chatOptions =
        new ChatOptions
        {
            Temperature = 0.0f,
            MaxOutputTokens = 800,
            TopP = 1.0f,
            PresencePenalty = 0.0f,
            FrequencyPenalty = 0.0f,
            ResponseFormat = ChatResponseFormat.Text
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

        var metric = new NumericMetric(RelevanceMetricName);
        var result = new EvaluationResult(metric);

        if (!messages.TryGetUserRequest(out ChatMessage? userRequest) || string.IsNullOrWhiteSpace(userRequest.Text))
        {
            metric.AddDiagnostics(
                EvaluationDiagnostic.Error(
                    $"The {nameof(messages)} supplied for evaluation did not contain a user request as the last message."));

            return result;
        }

        if (string.IsNullOrWhiteSpace(modelResponse.Text))
        {
            metric.AddDiagnostics(
                EvaluationDiagnostic.Error($"The {nameof(modelResponse)} supplied for evaluation was null or empty."));

            return result;
        }

        List<ChatMessage> evaluationInstructions = GetEvaluationInstructions(userRequest, modelResponse);

        (ChatResponse evaluationResponse, TimeSpan evaluationDuration) =
            await TimingHelper.ExecuteWithTimingAsync(() =>
                chatConfiguration.ChatClient.GetResponseAsync(
                    evaluationInstructions,
                    _chatOptions,
                    cancellationToken)).ConfigureAwait(false);

        _ = metric.TryParseEvaluationResponseWithTags(evaluationResponse, evaluationDuration);
        metric.Interpretation = metric.InterpretScore();
        return result;
    }

    private static List<ChatMessage> GetEvaluationInstructions(ChatMessage userRequest, ChatResponse modelResponse)
    {
#pragma warning disable S103 // Lines should not be too long
        const string SystemPrompt =
            """
            # Instruction
            ## Goal
            ### You are an expert in evaluating the quality of a RESPONSE from an intelligent system based on provided definition and data. Your goal will involve answering the questions below using the information provided.
            - **Definition**: You are given a definition of the communication trait that is being evaluated to help guide your Score.
            - **Data**: Your input data include QUERY and RESPONSE.
            - **Tasks**: To complete your evaluation you will be asked to evaluate the Data in different ways.
            """;
#pragma warning restore S103

        List<ChatMessage> evaluationInstructions = [new ChatMessage(ChatRole.System, SystemPrompt)];

        string renderedUserRequest = userRequest.RenderText();
        string renderedModelResponse = modelResponse.RenderText();

#pragma warning disable S103 // Lines should not be too long
        string evaluationPrompt =
            $$"""
            # Definition
            **Relevance** refers to how effectively a response addresses a question. It assesses the accuracy, completeness, and direct relevance of the response based solely on the given information.

            # Ratings
            ## [Relevance: 1] (Irrelevant Response)
            **Definition:** The response is unrelated to the question. It provides information that is off-topic and does not attempt to address the question posed.

            **Examples:**
              **Query:** What is the team preparing for?
              **Response:** I went grocery shopping yesterday evening.

              **Query:** When will the company's new product line launch?
              **Response:** International travel can be very rewarding and educational.

            ## [Relevance: 2] (Incorrect Response)
            **Definition:** The response attempts to address the question but includes incorrect information. It provides a response that is factually wrong based on the provided information.

            **Examples:**
              **Query:** When was the merger between the two firms finalized?
              **Response:** The merger was finalized on April 10th.     

              **Query:** Where and when will the solar eclipse be visible?
              **Response:** The solar eclipse will be visible in Asia on December 14th.

            ## [Relevance: 3] (Incomplete Response)
            **Definition:** The response addresses the question but omits key details necessary for a full understanding. It provides a partial response that lacks essential information.      

            **Examples:**
              **Query:** What type of food does the new restaurant offer?
              **Response:** The restaurant offers Italian food like pasta.

              **Query:** What topics will the conference cover?
              **Response:** The conference will cover renewable energy and climate change.

            ## [Relevance: 4] (Complete Response)
            **Definition:** The response fully addresses the question with accurate and complete information. It includes all essential details required for a comprehensive understanding, without adding any extraneous information.

            **Examples:**
              **Query:** What type of food does the new restaurant offer?
              **Response:** The new restaurant offers Italian cuisine, featuring dishes like pasta, pizza, and risotto.

              **Query:** What topics will the conference cover?
              **Response:** The conference will cover renewable energy, climate change, and sustainability practices.

            ## [Relevance: 5] (Comprehensive Response with Insights)    
            **Definition:** The response not only fully and accurately addresses the question but also includes additional relevant insights or elaboration. It may explain the significance, implications, or provide minor inferences that enhance understanding.

            **Examples:**
              **Query:** What type of food does the new restaurant offer?
              **Response:** The new restaurant offers Italian cuisine, featuring dishes like pasta, pizza, and risotto, aiming to provide customers with an authentic Italian dining experience.

              **Query:** What topics will the conference cover?
              **Response:** The conference will cover renewable energy, climate change, and sustainability practices, bringing together global experts to discuss these critical issues. 



            # Data
            QUERY: {{renderedUserRequest}}
            RESPONSE: {{renderedModelResponse}}


            # Tasks
            ## Please provide your assessment Score for the previous RESPONSE in relation to the QUERY based on the Definitions above. Your output should include the following information:
            - **ThoughtChain**: To improve the reasoning process, think step by step and include a step-by-step explanation of your thought process as you analyze the data based on the definitions. Keep it brief and start your ThoughtChain with "Let's think step by step:".
            - **Explanation**: a very short explanation of why you think the input Data should get that Score.
            - **Score**: based on your previous analysis, provide your Score. The Score you give MUST be a integer score (i.e., "1", "2"...) based on the levels of the definitions.


            ## Please provide your answers between the tags: <S0>your chain of thoughts</S0>, <S1>your explanation</S1>, <S2>your Score</S2>.
            # Output
            """;
#pragma warning restore S103

        evaluationInstructions.Add(new ChatMessage(ChatRole.User, evaluationPrompt));

        return evaluationInstructions;
    }
}
