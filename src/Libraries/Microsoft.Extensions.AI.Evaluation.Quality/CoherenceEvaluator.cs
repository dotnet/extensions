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
/// An <see cref="IEvaluator"/> that evaluates the 'Coherence' of a response produced by an AI model.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="CoherenceEvaluator"/> measures the readability and user-friendliness of the response being evaluated. It
/// assesses the ability of an AI system to generate text that reads naturally, flows smoothly, and resembles
/// human-like language in its responses.
/// </para>
/// <para>
/// <see cref="CoherenceEvaluator"/> returns a <see cref="NumericMetric"/> that contains a score for 'Coherence'. The
/// score is a number between 1 and 5, with 1 indicating a poor score, and 5 indicating an excellent score.
/// </para>
/// <para>
/// <b>Note:</b> <see cref="CoherenceEvaluator"/> is an AI-based evaluator that uses an AI model to perform its
/// evaluation. While the prompt that this evaluator uses to perform its evaluation is designed to be model-agnostic,
/// the performance of this prompt (and the resulting evaluation) can vary depending on the model used, and can be
/// especially poor when a smaller / local model is used.
/// </para>
/// <para>
/// The prompt that <see cref="CoherenceEvaluator"/> uses has been tested against (and tuned to work well with) the
/// following models. So, using this evaluator with a model from the following list is likely to produce the best
/// results. (The model to be used can be configured via <see cref="ChatConfiguration.ChatClient"/>.)
/// </para>
/// <para>
/// <b>GPT-4o</b>
/// </para>
/// </remarks>
/// <related type="Article" href="https://learn.microsoft.com/dotnet/ai/quickstarts/evaluate-ai-response">
/// Evaluate a model's response.
/// </related>
public sealed class CoherenceEvaluator : IEvaluator
{
    /// <summary>
    /// Gets the <see cref="EvaluationMetric.Name"/> of the <see cref="NumericMetric"/> returned by
    /// <see cref="CoherenceEvaluator"/>.
    /// </summary>
    public static string CoherenceMetricName => "Coherence";

    /// <inheritdoc/>
    public IReadOnlyCollection<string> EvaluationMetricNames { get; } = [CoherenceMetricName];

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

        var metric = new NumericMetric(CoherenceMetricName);
        var result = new EvaluationResult(metric);

        if (string.IsNullOrWhiteSpace(modelResponse.Text))
        {
            metric.AddDiagnostics(
                EvaluationDiagnostic.Error($"The {nameof(modelResponse)} supplied for evaluation was null or empty."));

            return result;
        }

        _ = messages.TryGetUserRequest(out ChatMessage? userRequest);

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

    private static List<ChatMessage> GetEvaluationInstructions(ChatMessage? userRequest, ChatResponse modelResponse)
    {
#pragma warning disable S103 // Lines should not be too long
        const string SystemPrompt =
            """
            # Instruction
            ## Goal
            ### You are an expert in evaluating the quality of a RESPONSE from an intelligent system based on provided definition and data. Your goal will involve answering the questions below using the information provided.
            - **Definition**: You are given a definition of the communication trait that is being evaluated to help guide your Score.
            - **Data**: Your input data include a QUERY and a RESPONSE.
            - **Tasks**: To complete your evaluation you will be asked to evaluate the Data in different ways.
            """;
#pragma warning restore S103

        List<ChatMessage> evaluationInstructions = [new ChatMessage(ChatRole.System, SystemPrompt)];

        string renderedUserRequest = userRequest?.RenderText() ?? string.Empty;
        string renderedModelResponse = modelResponse.RenderText();

#pragma warning disable S103 // Lines should not be too long
        string evaluationPrompt =
            $$"""
            # Definition
            **Coherence** refers to the logical and orderly presentation of ideas in a response, allowing the reader to easily follow and understand the writer's train of thought. A coherent answer directly addresses the question with clear connections between sentences and paragraphs, using appropriate transitions and a logical sequence of ideas.

            # Ratings
            ## [Coherence: 1] (Incoherent Response)
            **Definition:** The response lacks coherence entirely. It consists of disjointed words or phrases that do not form complete or meaningful sentences. There is no logical connection to the question, making the response incomprehensible.

            **Examples:**
              **Query:** What are the benefits of renewable energy?
              **Response:** Wind sun green jump apple silence over.

              **Query:** Explain the process of photosynthesis.
              **Response:** Plants light water flying blue music.

            ## [Coherence: 2] (Poorly Coherent Response)
            **Definition:** The response shows minimal coherence with fragmented sentences and limited connection to the question. It contains some relevant keywords but lacks logical structure and clear relationships between ideas, making the overall message difficult to understand.

            **Examples:**
              **Query:** How does vaccination work?
              **Response:** Vaccines protect disease. Immune system fight. Health better.

              **Query:** Describe how a bill becomes a law.
              **Response:** Idea proposed. Congress discuss vote. President signs.

            ## [Coherence: 3] (Partially Coherent Response)
            **Definition:** The response partially addresses the question with some relevant information but exhibits issues in the logical flow and organization of ideas. Connections between sentences may be unclear or abrupt, requiring the reader to infer the links. The response may lack smooth transitions and may present ideas out of order.

            **Examples:**
              **Query:** What causes earthquakes?
              **Response:** Earthquakes happen when tectonic plates move suddenly. Energy builds up then releases. Ground shakes and can cause damage.

              **Query:** Explain the importance of the water cycle.
              **Response:** The water cycle moves water around Earth. Evaporation, then precipitation occurs. It supports life by distributing water.

            ## [Coherence: 4] (Coherent Response)
            **Definition:** The response is coherent and effectively addresses the question. Ideas are logically organized with clear connections between sentences and paragraphs. Appropriate transitions are used to guide the reader through the response, which flows smoothly and is easy to follow.

            **Examples:**
              **Query:** What is the water cycle and how does it work?
              **Response:** The water cycle is the continuous movement of water on Earth through processes like evaporation, condensation, and precipitation. Water evaporates from bodies of water, forms clouds through condensation, and returns to the surface as precipitation. This cycle is essential for distributing water resources globally.

              **Query:** Describe the role of mitochondria in cellular function.
              **Response:** Mitochondria are organelles that produce energy for the cell. They convert nutrients into ATP through cellular respiration. This energy powers various cellular activities, making mitochondria vital for cell survival.

            ## [Coherence: 5] (Highly Coherent Response)
            **Definition:** The response is exceptionally coherent, demonstrating sophisticated organization and flow. Ideas are presented in a logical and seamless manner, with excellent use of transitional phrases and cohesive devices. The connections between concepts are clear and enhance the reader's understanding. The response thoroughly addresses the question with clarity and precision.

            **Examples:**
              **Query:** Analyze the economic impacts of climate change on coastal cities.
              **Response:** Climate change significantly affects the economies of coastal cities through rising sea levels, increased flooding, and more intense storms. These environmental changes can damage infrastructure, disrupt businesses, and lead to costly repairs. For instance, frequent flooding can hinder transportation and commerce, while the threat of severe weather may deter investment and tourism. Consequently, cities may face increased expenses for disaster preparedness and mitigation efforts, straining municipal budgets and impacting economic growth.

              **Query:** Discuss the significance of the Monroe Doctrine in shaping U.S. foreign policy.
              **Response:** The Monroe Doctrine was a pivotal policy declared in 1823 that asserted U.S. opposition to European colonization in the Americas. By stating that any intervention by external powers in the Western Hemisphere would be viewed as a hostile act, it established the U.S. as a protector of the region. This doctrine shaped U.S. foreign policy by promoting isolation from European conflicts while justifying American influence and expansion in the hemisphere. Its long-term significance lies in its enduring influence on international relations and its role in defining the U.S. position in global affairs.


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
