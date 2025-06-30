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
/// An <see cref="IEvaluator"/> that evaluates the 'Fluency' of a response produced by an AI model.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="FluencyEvaluator"/> measures the extent to which the response being evaluated is linguistically correct
/// (i.e., conforms to grammatical rules, syntactic structures, and appropriate vocabulary usage). It returns a
/// <see cref="NumericMetric"/> that contains a score for 'Fluency'. The score is a number between 1 and 5, with 1
/// indicating a poor score, and 5 indicating an excellent score.
/// </para>
/// <para>
/// <b>Note:</b> <see cref="FluencyEvaluator"/> is an AI-based evaluator that uses an AI model to perform its
/// evaluation. While the prompt that this evaluator uses to perform its evaluation is designed to be model-agnostic,
/// the performance of this prompt (and the resulting evaluation) can vary depending on the model used, and can be
/// especially poor when a smaller / local model is used.
/// </para>
/// <para>
/// The prompt that <see cref="FluencyEvaluator"/> uses has been tested against (and tuned to work well with) the
/// following models. So, using this evaluator with a model from the following list is likely to produce the best
/// results. (The model to be used can be configured via <see cref="ChatConfiguration.ChatClient"/>.)
/// </para>
/// <para>
/// <b>GPT-4o</b>
/// </para>
/// </remarks>
public sealed class FluencyEvaluator : IEvaluator
{
    /// <summary>
    /// Gets the <see cref="EvaluationMetric.Name"/> of the <see cref="NumericMetric"/> returned by
    /// <see cref="FluencyEvaluator"/>.
    /// </summary>
    public static string FluencyMetricName => "Fluency";

    /// <inheritdoc/>
    public IReadOnlyCollection<string> EvaluationMetricNames { get; } = [FluencyMetricName];

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

        var metric = new NumericMetric(FluencyMetricName);
        var result = new EvaluationResult(metric);

        if (string.IsNullOrWhiteSpace(modelResponse.Text))
        {
            metric.AddDiagnostics(
                EvaluationDiagnostic.Error($"The {nameof(modelResponse)} supplied for evaluation was null or empty."));

            return result;
        }

        List<ChatMessage> evaluationInstructions = GetEvaluationInstructions(modelResponse);

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

    private static List<ChatMessage> GetEvaluationInstructions(ChatResponse modelResponse)
    {
#pragma warning disable S103 // Lines should not be too long
        const string SystemPrompt =
            """
            # Instruction
            ## Goal
            ### You are an expert in evaluating the quality of a RESPONSE from an intelligent system based on provided definition and data. Your goal will involve answering the questions below using the information provided.
            - **Definition**: You are given a definition of the communication trait that is being evaluated to help guide your Score.
            - **Data**: Your input data include a RESPONSE.
            - **Tasks**: To complete your evaluation you will be asked to evaluate the Data in different ways.
            """;
#pragma warning restore S103

        List<ChatMessage> evaluationInstructions = [new ChatMessage(ChatRole.System, SystemPrompt)];

        string renderedModelResponse = modelResponse.RenderText();

#pragma warning disable S103 // Lines should not be too long
        string evaluationPrompt =
            $$"""
            # Definition
            **Fluency** refers to the effectiveness and clarity of written communication, focusing on grammatical accuracy, vocabulary range, sentence complexity, coherence, and overall readability. It assesses how smoothly ideas are conveyed and how easily the text can be understood by the reader.

            # Ratings
            ## [Fluency: 1] (Emergent Fluency)
            **Definition:** The response shows minimal command of the language. It contains pervasive grammatical errors, extremely limited vocabulary, and fragmented or incoherent sentences. The message is largely incomprehensible, making understanding very difficult.

            **Examples:**
              **Response:** Free time I. Go park. Not fun. Alone.

              **Response:** Like food pizza. Good cheese eat.

            ## [Fluency: 2] (Basic Fluency)
            **Definition:** The response communicates simple ideas but has frequent grammatical errors and limited vocabulary. Sentences are short and may be improperly constructed, leading to partial understanding. Repetition and awkward phrasing are common.

            **Examples:**
              **Response:** I like play soccer. I watch movie. It fun.

              **Response:** My town small. Many people. We have market.

            ## [Fluency: 3] (Competent Fluency)
            **Definition:** The response clearly conveys ideas with occasional grammatical errors. Vocabulary is adequate but not extensive. Sentences are generally correct but may lack complexity and variety. The text is coherent, and the message is easily understood with minimal effort.

            **Examples:**
              **Response:** I'm planning to visit friends and maybe see a movie together.

              **Response:** I try to eat healthy food and exercise regularly by jogging.

            ## [Fluency: 4] (Proficient Fluency)
            **Definition:** The response is well-articulated with good control of grammar and a varied vocabulary. Sentences are complex and well-structured, demonstrating coherence and cohesion. Minor errors may occur but do not affect overall understanding. The text flows smoothly, and ideas are connected logically.

            **Examples:**
              **Response:** My interest in mathematics and problem-solving inspired me to become an engineer, as I enjoy designing solutions that improve people's lives.

              **Response:** Environmental conservation is crucial because it protects ecosystems, preserves biodiversity, and ensures natural resources are available for future generations.

            ## [Fluency: 5] (Exceptional Fluency)
            **Definition:** The response demonstrates an exceptional command of language with sophisticated vocabulary and complex, varied sentence structures. It is coherent, cohesive, and engaging, with precise and nuanced expression. Grammar is flawless, and the text reflects a high level of eloquence and style.

            **Examples:**
              **Response:** Globalization exerts a profound influence on cultural diversity by facilitating unprecedented cultural exchange while simultaneously risking the homogenization of distinct cultural identities, which can diminish the richness of global heritage.

              **Response:** Technology revolutionizes modern education by providing interactive learning platforms, enabling personalized learning experiences, and connecting students worldwide, thereby transforming how knowledge is acquired and shared.


            # Data
            RESPONSE: {{renderedModelResponse}}


            # Tasks
            ## Please provide your assessment Score for the previous RESPONSE based on the Definitions above. Your output should include the following information:
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
