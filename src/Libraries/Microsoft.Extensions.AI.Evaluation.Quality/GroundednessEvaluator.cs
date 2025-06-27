// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.Evaluation.Utilities;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.Quality;

/// <summary>
/// An <see cref="IEvaluator"/> that evaluates the 'Groundedness' of a response produced by an AI model.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="GroundednessEvaluator"/> measures the degree to which the response being evaluated is grounded in the
/// information present in the supplied <see cref="GroundednessEvaluatorContext.GroundingContext"/>. It returns a
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
public sealed class GroundednessEvaluator : IEvaluator
{
    /// <summary>
    /// Gets the <see cref="EvaluationMetric.Name"/> of the <see cref="NumericMetric"/> returned by
    /// <see cref="GroundednessEvaluator"/>.
    /// </summary>
    public static string GroundednessMetricName => "Groundedness";

    /// <inheritdoc/>
    public IReadOnlyCollection<string> EvaluationMetricNames { get; } = [GroundednessMetricName];

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

        var metric = new NumericMetric(GroundednessMetricName);
        var result = new EvaluationResult(metric);

        if (string.IsNullOrWhiteSpace(modelResponse.Text))
        {
            metric.AddDiagnostics(
                EvaluationDiagnostic.Error($"The {nameof(modelResponse)} supplied for evaluation was null or empty."));

            return result;
        }

        if (additionalContext?.OfType<GroundednessEvaluatorContext>().FirstOrDefault()
                is not GroundednessEvaluatorContext context)
        {
            metric.AddDiagnostics(
                EvaluationDiagnostic.Error(
                    $"A value of type {nameof(GroundednessEvaluatorContext)} was not found in the {nameof(additionalContext)} collection."));

            return result;
        }

        _ = messages.TryGetUserRequest(out ChatMessage? userRequest);

        List<ChatMessage> evaluationInstructions =
            GetEvaluationInstructions(userRequest, modelResponse, context);

        (ChatResponse evaluationResponse, TimeSpan evaluationDuration) =
            await TimingHelper.ExecuteWithTimingAsync(() =>
                chatConfiguration.ChatClient.GetResponseAsync(
                    evaluationInstructions,
                    _chatOptions,
                    cancellationToken)).ConfigureAwait(false);

        _ = metric.TryParseEvaluationResponseWithTags(evaluationResponse, evaluationDuration);
        metric.AddOrUpdateContext(context);
        metric.Interpretation = metric.InterpretScore();
        return result;
    }

    private static List<ChatMessage> GetEvaluationInstructions(
        ChatMessage? userRequest,
        ChatResponse modelResponse,
        GroundednessEvaluatorContext context)
    {
#pragma warning disable S103 // Lines should not be too long
        const string SystemPrompt =
            """
            # Instruction
            ## Goal
            ### You are an expert in evaluating the quality of a RESPONSE from an intelligent system based on provided definition and data. Your goal will involve answering the questions below using the information provided.
            - **Definition**: You are given a definition of the communication trait that is being evaluated to help guide your Score.
            - **Data**: Your input data include CONTEXT, RESPONSE and an optional QUERY.
            - **Tasks**: To complete your evaluation you will be asked to evaluate the Data in different ways.
            """;
#pragma warning restore S103

        List<ChatMessage> evaluationInstructions = [new ChatMessage(ChatRole.System, SystemPrompt)];

        string renderedModelResponse = modelResponse.RenderText();
        string groundingContext = context.GroundingContext;

#pragma warning disable S103 // Lines should not be too long
        string evaluationPrompt;
        if (userRequest is null)
        {
            evaluationPrompt =
                $$"""
                # Definition
                **Groundedness** refers to how faithfully a response adheres to the information provided in the CONTEXT, ensuring that all content is directly supported by the context without introducing unsupported information or omitting critical details. It evaluates the fidelity and precision of the response in relation to the source material.

                # Ratings
                ## [Groundedness: 1] (Completely Ungrounded Response)
                **Definition:** The response is entirely unrelated to the CONTEXT, introducing topics or information that have no connection to the provided material.

                **Examples:**
                  **Context:** The company's profits increased by 20% in the last quarter.
                  **Response:** I enjoy playing soccer on weekends with my friends.

                  **Context:** The new smartphone model features a larger display and improved battery life.
                  **Response:** The history of ancient Egypt is fascinating and full of mysteries.

                ## [Groundedness: 2] (Contradictory Response)
                **Definition:** The response directly contradicts or misrepresents the information provided in the CONTEXT.

                **Examples:**
                  **Context:** The company's profits increased by 20% in the last quarter.
                  **Response:** The company's profits decreased by 20% in the last quarter.

                  **Context:** The new smartphone model features a larger display and improved battery life.
                  **Response:** The new smartphone model has a smaller display and shorter battery life.

                ## [Groundedness: 3] (Accurate Response with Unsupported Additions)
                **Definition:** The response accurately includes information from the CONTEXT but adds details, opinions, or explanations that are not supported by the provided material.

                **Examples:**
                  **Context:** The company's profits increased by 20% in the last quarter.
                  **Response:** The company's profits increased by 20% in the last quarter due to their aggressive marketing strategy.

                  **Context:** The new smartphone model features a larger display and improved battery life.
                  **Response:** The new smartphone model features a larger display, improved battery life, and comes with a free case.

                ## [Groundedness: 4] (Incomplete Response Missing Critical Details)
                **Definition:** The response contains information from the CONTEXT but omits essential details that are necessary for a comprehensive understanding of the main point.

                **Examples:**
                  **Context:** The company's profits increased by 20% in the last quarter, marking the highest growth rate in its history.      
                  **Response:** The company's profits increased by 20% in the last quarter.

                  **Context:** The new smartphone model features a larger display, improved battery life, and an upgraded camera system.        
                  **Response:** The new smartphone model features a larger display and improved battery life.

                ## [Groundedness: 5] (Fully Grounded and Complete Response)
                **Definition:** The response is entirely based on the CONTEXT, accurately and thoroughly conveying all essential information without introducing unsupported details or omitting critical points.

                **Examples:**
                  **Context:** The company's profits increased by 20% in the last quarter, marking the highest growth rate in its history.      
                  **Response:** The company's profits increased by 20% in the last quarter, marking the highest growth rate in its history.     

                  **Context:** The new smartphone model features a larger display, improved battery life, and an upgraded camera system.        
                  **Response:** The new smartphone model features a larger display, improved battery life, and an upgraded camera system.  


                # Data
                CONTEXT: {{groundingContext}}
                RESPONSE: {{renderedModelResponse}}


                # Tasks
                ## Please provide your assessment Score for the previous RESPONSE in relation to the CONTEXT based on the Definitions above. Your output should include the following information:
                - **ThoughtChain**: To improve the reasoning process, think step by step and include a step-by-step explanation of your thought process as you analyze the data based on the definitions. Keep it brief and start your ThoughtChain with "Let's think step by step:".
                - **Explanation**: a very short explanation of why you think the input Data should get that Score.
                - **Score**: based on your previous analysis, provide your Score. The Score you give MUST be a integer score (i.e., "1", "2"...) based on the levels of the definitions.


                ## Please provide your answers between the tags: <S0>your chain of thoughts</S0>, <S1>your explanation</S1>, <S2>your Score</S2>.
                # Output
                """;
        }
        else
        {
            string renderedUserRequest = userRequest.RenderText();

            evaluationPrompt =
                $$"""
                # Definition
                **Groundedness** refers to how well an answer is anchored in the provided context, evaluating its relevance, accuracy, and completeness based exclusively on that context. It assesses the extent to which the answer directly and fully addresses the question without introducing unrelated or incorrect information. The scale ranges from 1 to 5, with higher numbers indicating greater groundedness.

                # Ratings
                ## [Groundedness: 1] (Completely Unrelated Response)
                **Definition:** An answer that does not relate to the question or the context in any way. It fails to address the topic, provides irrelevant information, or introduces completely unrelated subjects.

                **Examples:**
                  **Context:** The company's annual meeting will be held next Thursday.
                  **Query:** When is the company's annual meeting?
                  **Response:** I enjoy hiking in the mountains during summer.

                  **Context:** The new policy aims to reduce carbon emissions by 20% over the next five years.
                  **Query:** What is the goal of the new policy?
                  **Response:** My favorite color is blue.

                ## [Groundedness: 2] (Related Topic but Does Not Respond to the Query)
                **Definition:** An answer that relates to the general topic of the context but does not answer the specific question asked. It may mention concepts from the context but fails to provide a direct or relevant response.

                **Examples:**
                  **Context:** The museum will exhibit modern art pieces from various local artists.
                  **Query:** What kind of art will be exhibited at the museum?
                  **Response:** Museums are important cultural institutions.

                  **Context:** The new software update improves battery life and performance.
                  **Query:** What does the new software update improve?
                  **Response:** Software updates can sometimes fix bugs.

                ## [Groundedness: 3] (Attempts to Respond but Contains Incorrect Information)
                **Definition:** An answer that attempts to respond to the question but includes incorrect information not supported by the context. It may misstate facts, misinterpret the context, or provide erroneous details.

                **Examples:**
                  **Context:** The festival starts on June 5th and features international musicians.
                  **Query:** When does the festival start?
                  **Response:** The festival starts on July 5th and features local artists.

                  **Context:** The recipe requires two eggs and one cup of milk.
                  **Query:** How many eggs are needed for the recipe?
                  **Response:** You need three eggs for the recipe.

                ## [Groundedness: 4] (Partially Correct Response)
                **Definition:** An answer that provides a correct response to the question but is incomplete or lacks specific details mentioned in the context. It captures some of the necessary information but omits key elements needed for a full understanding.

                **Examples:**
                  **Context:** The bookstore offers a 15% discount to students and a 10% discount to senior citizens.
                  **Query:** What discount does the bookstore offer to students?
                  **Response:** Students get a discount at the bookstore.

                  **Context:** The company's headquarters are located in Berlin, Germany.
                  **Query:** Where are the company's headquarters?
                  **Response:** The company's headquarters are in Germany.

                ## [Groundedness: 5] (Fully Correct and Complete Response)
                **Definition:** An answer that thoroughly and accurately responds to the question, including all relevant details from the context. It directly addresses the question with precise information, demonstrating complete understanding without adding extraneous information.

                **Examples:**
                  **Context:** The author released her latest novel, 'The Silent Echo', on September 1st.
                  **Query:** When was 'The Silent Echo' released?
                  **Response:** 'The Silent Echo' was released on September 1st.

                  **Context:** Participants must register by May 31st to be eligible for early bird pricing.
                  **Query:** By what date must participants register to receive early bird pricing?
                  **Response:** Participants must register by May 31st to receive early bird pricing.


                # Data
                CONTEXT: {{groundingContext}}
                QUERY: {{renderedUserRequest}}
                RESPONSE: {{renderedModelResponse}}


                # Tasks
                ## Please provide your assessment Score for the previous RESPONSE in relation to the CONTEXT and QUERY based on the Definitions above. Your output should include the following information:
                - **ThoughtChain**: To improve the reasoning process, think step by step and include a step-by-step explanation of your thought process as you analyze the data based on the definitions. Keep it brief and start your ThoughtChain with "Let's think step by step:".
                - **Explanation**: a very short explanation of why you think the input Data should get that Score.
                - **Score**: based on your previous analysis, provide your Score. The Score you give MUST be a integer score (i.e., "1", "2"...) based on the levels of the definitions.


                ## Please provide your answers between the tags: <S0>your chain of thoughts</S0>, <S1>your explanation</S1>, <S2>your Score</S2>.
                # Output
                """;
        }
#pragma warning restore S103 

        evaluationInstructions.Add(new ChatMessage(ChatRole.User, evaluationPrompt));

        return evaluationInstructions;
    }
}
