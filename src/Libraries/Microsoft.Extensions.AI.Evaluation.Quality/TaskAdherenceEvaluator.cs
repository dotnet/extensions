// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.Evaluation.Utilities;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.Quality;

/// <summary>
/// An <see cref="IEvaluator"/> that evaluates an AI system's effectiveness at adhering to the task assigned to it.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TaskAdherenceEvaluator"/> measures how accurately an AI system adheres to the task assigned to it by
/// examining the alignment of the supplied response with instructions and definitions present in the conversation
/// history, the accuracy and clarity of the response, and the proper use of tool definitions supplied via
/// <see cref="TaskAdherenceEvaluatorContext.ToolDefinitions"/>.
/// </para>
/// <para>
/// Note that at the moment, <see cref="TaskAdherenceEvaluator"/> only supports evaluating calls to tools that are
/// defined as <see cref="AIFunction"/>s. Any other <see cref="AITool"/> definitions that are supplied via
/// <see cref="TaskAdherenceEvaluatorContext.ToolDefinitions"/> will be ignored.
/// </para>
/// <para>
/// <see cref="TaskAdherenceEvaluator"/> returns a <see cref="NumericMetric"/> that contains a score for 'Task
/// Adherence'. The score is a number between 1 and 5, with 1 indicating a poor score, and 5 indicating an excellent
/// score.
/// </para>
/// <para>
/// <b>Note:</b> <see cref="TaskAdherenceEvaluator"/> is an AI-based evaluator that uses an AI model to perform its
/// evaluation. While the prompt that this evaluator uses to perform its evaluation is designed to be model-agnostic,
/// the performance of this prompt (and the resulting evaluation) can vary depending on the model used, and can be
/// especially poor when a smaller / local model is used.
/// </para>
/// <para>
/// The prompt that <see cref="TaskAdherenceEvaluator"/> uses has been tested against (and tuned to work well with)
/// the following models. So, using this evaluator with a model from the following list is likely to produce the best
/// results. (The model to be used can be configured via <see cref="ChatConfiguration.ChatClient"/>.)
/// </para>
/// <para>
/// <b>GPT-4o</b>
/// </para>
/// </remarks>
[Experimental("AIEVAL001")]
public sealed class TaskAdherenceEvaluator : IEvaluator
{
    /// <summary>
    /// Gets the <see cref="EvaluationMetric.Name"/> of the <see cref="NumericMetric"/> returned by
    /// <see cref="TaskAdherenceEvaluator"/>.
    /// </summary>
    public static string TaskAdherenceMetricName => "Task Adherence";

    /// <inheritdoc/>
    public IReadOnlyCollection<string> EvaluationMetricNames { get; } = [TaskAdherenceMetricName];

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

        var metric = new NumericMetric(TaskAdherenceMetricName);
        var result = new EvaluationResult(metric);

        if (!messages.Any())
        {
            metric.AddDiagnostics(
                EvaluationDiagnostic.Error(
                    "The conversation history supplied for evaluation did not include any messages."));

            return result;
        }

        if (!modelResponse.Messages.Any())
        {
            metric.AddDiagnostics(
                EvaluationDiagnostic.Error(
                    $"The {nameof(modelResponse)} supplied for evaluation did not include any messages."));

            return result;
        }

        TaskAdherenceEvaluatorContext? context =
            additionalContext?.OfType<TaskAdherenceEvaluatorContext>().FirstOrDefault();

        if (context is not null && context.ToolDefinitions.Count is 0)
        {
            metric.AddDiagnostics(
                EvaluationDiagnostic.Error(
                    $"Supplied {nameof(TaskAdherenceEvaluatorContext)} did not contain any {nameof(TaskAdherenceEvaluatorContext.ToolDefinitions)}."));

            return result;
        }

        var toolDefinitionNames = new HashSet<string>(context?.ToolDefinitions.Select(td => td.Name) ?? []);
        IEnumerable<FunctionCallContent> toolCalls =
            modelResponse.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>();

        if (toolCalls.Any(t => !toolDefinitionNames.Contains(t.Name)))
        {
            if (context is null)
            {
                metric.AddDiagnostics(
                    EvaluationDiagnostic.Error(
                        $"The {nameof(modelResponse)} supplied for evaluation contained calls to tools that were not supplied via {nameof(TaskAdherenceEvaluatorContext)}."));
            }
            else
            {
                metric.AddDiagnostics(
                    EvaluationDiagnostic.Error(
                        $"The {nameof(modelResponse)} supplied for evaluation contained calls to tools that were not included in the supplied {nameof(TaskAdherenceEvaluatorContext)}."));
            }

            return result;
        }

        List<ChatMessage> evaluationInstructions = GetEvaluationInstructions(messages, modelResponse, context);

        (ChatResponse evaluationResponse, TimeSpan evaluationDuration) =
            await TimingHelper.ExecuteWithTimingAsync(() =>
                chatConfiguration.ChatClient.GetResponseAsync(
                    evaluationInstructions,
                    _chatOptions,
                    cancellationToken)).ConfigureAwait(false);

        _ = metric.TryParseEvaluationResponseWithTags(evaluationResponse, evaluationDuration);

        if (context is not null)
        {
            metric.AddOrUpdateContext(context);
        }

        metric.Interpretation = metric.InterpretScore();
        return result;
    }

    private static List<ChatMessage> GetEvaluationInstructions(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        TaskAdherenceEvaluatorContext? context)
    {
        string renderedConversation = messages.RenderAsJson();
        string renderedModelResponse = modelResponse.RenderAsJson();
        string? renderedToolDefinitions = context?.ToolDefinitions.RenderAsJson();

#pragma warning disable S103 // Lines should not be too long
        string systemPrompt =
            $$"""
            # Instruction
            ## Context
            ### You are an expert in evaluating the quality of an answer from an intelligent system based on provided definitions and data. Your goal will involve answering the questions below using the information provided.
            - **Definition**: Based on the provided query, response, and tool definitions, evaluate the agent's adherence to the assigned task. 
            - **Data**: Your input data includes query, response, and tool definitions.
            - **Questions**: To complete your evaluation you will be asked to evaluate the Data in different ways.

            # Definition

            **Level 1: Fully Inadherent**

            **Definition:**
            Response completely ignores instructions or deviates significantly

            **Example:**
              **Query:** What is a recommended weekend itinerary in Paris?
              **Response:** Paris is a lovely city with a rich history.

            Explanation: This response completely misses the task by not providing any itinerary details. It offers a generic statement about Paris rather than a structured travel plan.


            **Level 2: Barely Adherent**

            **Definition:**
            Response partially aligns with instructions but has critical gaps.

            **Example:**
              **Query:** What is a recommended weekend itinerary in Paris?
              **Response:** Spend your weekend visiting famous places in Paris.

            Explanation: While the response hints at visiting well-known sites, it is extremely vague and lacks specific details, such as which sites to visit or any order of activities, leaving major gaps in the instructions.


            **Level 3: Moderately Adherent**

            **Definition:**
            Response meets the core requirements but lacks precision or clarity.

            **Example:**
              **Query:** What is a recommended weekend itinerary in Paris?
              **Response:** Visit the Eiffel Tower and the Louvre on Saturday, and stroll through Montmartre on Sunday.

            Explanation: This answer meets the basic requirement by naming a few key attractions and assigning them to specific days. However, it lacks additional context, such as timings, additional activities, or details to make the itinerary practical and clear.


            **Level 4: Mostly Adherent**

            **Definition:**
            Response is clear, accurate, and aligns with instructions with minor issues.

            **Example:**
              **Query:** What is a recommended weekend itinerary in Paris?
              **Response:** For a weekend in Paris, start Saturday with a morning visit to the Eiffel Tower, then head to the Louvre in the early afternoon. In the evening, enjoy a leisurely walk along the Seine. On Sunday, begin with a visit to Notre-Dame Cathedral, followed by exploring the art and cafés in Montmartre. This plan offers a mix of cultural visits and relaxing experiences.

            Explanation: This response is clear, structured, and provides a concrete itinerary with specific attractions and a suggested order of activities. It is accurate and useful, though it might benefit from a few more details like exact timings or restaurant suggestions to be perfect.


            **Level 5: Fully Adherent**

            **Definition:**
            Response is flawless, accurate, and follows instructions to the letter.

            **Example:**
              **Query:** What is a recommended weekend itinerary in Paris?
              **Response:** Here is a detailed weekend itinerary in Paris:
            Saturday:
            Morning: Begin your day with a visit to the Eiffel Tower to admire the views from the top.
            Early Afternoon: Head to the Louvre for a guided tour of its most famous exhibits.
            Late Afternoon: Take a relaxing walk along the Seine, stopping at local boutiques.
            Evening: Enjoy dinner at a classic Parisian bistro near the river.
            Sunday:
            Morning: Visit the Notre-Dame Cathedral to explore its architecture and history.
            Midday: Wander the charming streets of Montmartre, stopping by art galleries and cafés.
            Afternoon: Finish your trip with a scenic boat tour on the Seine.
            This itinerary balances cultural immersion, leisure, and local dining experiences, ensuring a well-rounded visit.

            Explanation:  This response is comprehensive and meticulously follows the instructions. It provides detailed steps, timings, and a variety of activities that fully address the query, leaving no critical gaps.

            # Data
            Query: {{renderedConversation}}
            Response: {{renderedModelResponse}}
            Tool Definitions: {{renderedToolDefinitions}}

            # Tasks
            ## Please provide your assessment Score for the previous answer. Your output should include the following information:
            - **ThoughtChain**: To improve the reasoning process, Think Step by Step and include a step-by-step explanation of your thought process as you analyze the data based on the definitions. Keep it brief and Start your ThoughtChain with "Let's think step by step:".
            - **Explanation**: a very short explanation of why you think the input data should get that Score.
            - **Score**: based on your previous analysis, provide your Score. The answer you give MUST be an integer score ("1", "2", ...) based on the categories of the definitions.

            ## Please provide your answers between the tags: <S0>your chain of thoughts</S0>, <S1>your explanation</S1>, <S2>your score</S2>.
            # Output
            """;
#pragma warning restore S103

        List<ChatMessage> evaluationInstructions = [new ChatMessage(ChatRole.System, systemPrompt)];
        return evaluationInstructions;
    }
}
