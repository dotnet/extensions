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
/// An <see cref="IEvaluator"/> that evaluates an AI system's effectiveness at identifying and resolving user intent.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IntentResolutionEvaluator"/> evaluates an AI system's effectiveness at identifying and resolving user
/// intent based on the supplied conversation history and the tool definitions supplied via
/// <see cref="IntentResolutionEvaluatorContext.ToolDefinitions"/>.
/// </para>
/// <para>
/// Note that at the moment, <see cref="IntentResolutionEvaluator"/> only supports evaluating calls to tools that are
/// defined as <see cref="AIFunction"/>s. Any other <see cref="AITool"/> definitions that are supplied via
/// <see cref="IntentResolutionEvaluatorContext.ToolDefinitions"/> will be ignored.
/// </para>
/// <para>
/// <see cref="IntentResolutionEvaluator"/> returns a <see cref="NumericMetric"/> that contains a score for 'Intent
/// Resolution'. The score is a number between 1 and 5, with 1 indicating a poor score, and 5 indicating an excellent
/// score.
/// </para>
/// <para>
/// <b>Note:</b> <see cref="IntentResolutionEvaluator"/> is an AI-based evaluator that uses an AI model to perform its
/// evaluation. While the prompt that this evaluator uses to perform its evaluation is designed to be model-agnostic,
/// the performance of this prompt (and the resulting evaluation) can vary depending on the model used, and can be
/// especially poor when a smaller / local model is used.
/// </para>
/// <para>
/// The prompt that <see cref="IntentResolutionEvaluator"/> uses has been tested against (and tuned to work well with)
/// the following models. So, using this evaluator with a model from the following list is likely to produce the best
/// results. (The model to be used can be configured via <see cref="ChatConfiguration.ChatClient"/>.)
/// </para>
/// <para>
/// <b>GPT-4o</b>
/// </para>
/// </remarks>
[Experimental("AIEVAL001")]
public sealed class IntentResolutionEvaluator : IEvaluator
{
    /// <summary>
    /// Gets the <see cref="EvaluationMetric.Name"/> of the <see cref="NumericMetric"/> returned by
    /// <see cref="IntentResolutionEvaluator"/>.
    /// </summary>
    public static string IntentResolutionMetricName => "Intent Resolution";

    /// <inheritdoc/>
    public IReadOnlyCollection<string> EvaluationMetricNames { get; } = [IntentResolutionMetricName];

    private static readonly ChatOptions _chatOptions =
        new ChatOptions
        {
            Temperature = 0.0f,
            MaxOutputTokens = 800,
            TopP = 1.0f,
            PresencePenalty = 0.0f,
            FrequencyPenalty = 0.0f,
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

        var metric = new NumericMetric(IntentResolutionMetricName);
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

        IntentResolutionEvaluatorContext? context =
            additionalContext?.OfType<IntentResolutionEvaluatorContext>().FirstOrDefault();

        if (context is not null && context.ToolDefinitions.Count is 0)
        {
            metric.AddDiagnostics(
                EvaluationDiagnostic.Error(
                    $"Supplied {nameof(IntentResolutionEvaluatorContext)} did not contain any {nameof(IntentResolutionEvaluatorContext.ToolDefinitions)}."));

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
                        $"The {nameof(modelResponse)} supplied for evaluation contained calls to tools that were not supplied via {nameof(IntentResolutionEvaluatorContext)}."));
            }
            else
            {
                metric.AddDiagnostics(
                    EvaluationDiagnostic.Error(
                        $"The {nameof(modelResponse)} supplied for evaluation contained calls to tools that were not included in the supplied {nameof(IntentResolutionEvaluatorContext)}."));
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

        if (context is not null)
        {
            metric.AddOrUpdateContext(context);
        }

        await ParseEvaluationResponseAsync(
            metric,
            evaluationResponse,
            evaluationDuration,
            chatConfiguration,
            cancellationToken).ConfigureAwait(false);

        return result;
    }

    private static List<ChatMessage> GetEvaluationInstructions(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        IntentResolutionEvaluatorContext? context)
    {
        const string SystemPrompt =
            "You are an expert in evaluating the quality of a RESPONSE from an intelligent assistant based on provided definition and Data.";

        List<ChatMessage> evaluationInstructions = [new ChatMessage(ChatRole.System, SystemPrompt)];

        string renderedConversation = messages.RenderAsJson();
        string renderedModelResponse = modelResponse.RenderAsJson();
        string? renderedToolDefinitions = context?.ToolDefinitions.RenderAsJson();

#pragma warning disable S103 // Lines should not be too long
        string evaluationPrompt =
            $$"""
            # Goal
            Your goal is to assess the quality of the RESPONSE of an assistant in relation to a QUERY from a user, specifically focusing on
            the assistant's ability to understand and resolve the user intent expressed in the QUERY. There is also a field for tool definitions
            describing the functions, if any, that are accessible to the agent and that the agent may invoke in the RESPONSE if necessary.

            There are two components to intent resolution:
                - Intent Understanding: The extent to which the agent accurately discerns the user's underlying need or inquiry.
                - Response Resolution: The degree to which the agent's response is comprehensive, relevant, and adequately addresses the user's request.

            Note that the QUERY can either be a string with a user request or an entire conversation history including previous requests and responses from the assistant.
            In this case, the assistant's response should be evaluated in the context of the entire conversation but the focus should be on the last intent.

            # Data
            QUERY: {{renderedConversation}}
            RESPONSE: {{renderedModelResponse}}
            TOOL_DEFINITIONS: {{renderedToolDefinitions}}


            # Ratings
            ## [Score: 1] (Response completely unrelated to user intent)
            **Definition:** The agent's response does not address the query at all.

            **Example:**
              **Query:** How do I bake a chocolate cake?
              **Response:** The latest smartphone models have incredible features and performance.
              **Tool Definitions:** []

            **Expected output**
            {
                "explanation": "The agent's response is entirely off-topic, discussing smartphones instead of providing any information about baking a chocolate cake."
                "conversation_has_intent": true,
                "agent_perceived_intent": "discussion about smartphone features",
                "actual_user_intent": "bake a chocolate cake",
                "correct_intent_detected": false,
                "intent_resolved": false,
                "resolution_score": 1,
            }


            ## [Score: 2] (Response minimally relates to user intent)
            **Definition:** The response shows a token attempt to address the query by mentioning a relevant keyword or concept, but it provides almost no useful or actionable information.

            **Example input:**
              **Query:** How do I bake a chocolate cake?
              **Response:** Chocolate cake involves some ingredients.
              **Tool Definitions:** []

            **Expected output**
            {
                "explanation": "While the response mentions 'ingredients' related to a chocolate cake, it barely addresses the process or any detailed steps, leaving the query unresolved."
                "conversation_has_intent": true,
                "agent_perceived_intent": "mention of ingredients",
                "actual_user_intent": "bake a chocolate cake",
                "correct_intent_detected": false,
                "intent_resolved": false,
                "resolution_score": 2,
            }


            ## [Score: 3] (Response partially addresses the user intent but lacks complete details)
            **Definition:** The response provides a basic idea related to the query by mentioning a few relevant elements, but it omits several key details and specifics needed for fully resolving the user's query.

            **Example input:**
              **Query:** How do I bake a chocolate cake?
              **Response:** Preheat your oven and mix the ingredients before baking the cake.
              **Tool Definitions:** []

            **Expected output**
            {
                "explanation": "The response outlines a minimal process (preheating and mixing) but omits critical details like ingredient measurements, baking time, and temperature specifics, resulting in only a partial resolution of the query."
                "conversation_has_intent": true,
                "agent_perceived_intent": "basic baking process",
                "actual_user_intent": "bake a chocolate cake",
                "correct_intent_detected": true,
                "intent_resolved": false,
                "resolution_score": 3,
            }


            ## [Score: 4] (Response addresses the user intent with moderate accuracy but has minor inaccuracies or omissions)
            **Definition:** The response offers a moderately detailed answer that includes several specific elements relevant to the query, yet it still lacks some finer details or complete information.

            **Example input:**
              **Query:** How do I bake a chocolate cake?
              **Response:** Preheat your oven to 350°F. In a bowl, combine flour, sugar, cocoa, eggs, and milk, mix well, and bake for about 30 minutes.
              **Tool Definitions:** []

            **Expected output**
            {
                "explanation": "The response includes specific steps and ingredients, indicating a clear intent to provide a recipe. However, it lacks detailed measurements and finer instructions, which makes the resolution only moderately complete."
                "conversation_has_intent": true,
                "agent_perceived_intent": "provide a basic chocolate cake recipe",
                "actual_user_intent": "bake a chocolate cake",
                "correct_intent_detected": true,
                "intent_resolved": false,
                "resolution_score": 4,
            }


            ## [Score: 5] (Response directly addresses the user intent and fully resolves it)
            **Definition:** The response provides a complete, detailed, and accurate answer that fully resolves the user's query with all necessary information and precision.

            **Example input:**
              **Query:** How do I bake a chocolate cake?
              **Response:** Preheat your oven to 350°F (175°C) and grease a 9-inch round cake pan. In a large bowl, sift together 1 ¾ cups all-purpose flour, 1 cup sugar, ¾ cup unsweetened cocoa powder, 1 ½ tsp baking powder, and 1 tsp salt. In another bowl, beat 2 large eggs with 1 cup milk, ½ cup vegetable oil, and 2 tsp vanilla extract. Combine the wet ingredients with the dry ingredients, then gradually mix in 1 cup boiling water until smooth. Pour the batter into the prepared pan and bake for 30-35 minutes or until a toothpick inserted into the center comes out clean. Allow the cake to cool before serving.
              **Tool Definitions:** []

            **Expected output**
            {
                "explanation": "The response delivers a complete and precise recipe with detailed instructions and measurements, fully addressing the user's query about baking a chocolate cake."
                "conversation_has_intent": true,
                "agent_perceived_intent": "provide a comprehensive chocolate cake recipe",
                "actual_user_intent": "bake a chocolate cake",
                "correct_intent_detected": true,
                "intent_resolved": true,
                "resolution_score": 5,
            }


            # Task

            Please provide your evaluation for the assistant RESPONSE in relation to the user QUERY and tool definitions based on the Definitions and examples above.
            Your output should consist only of a JSON object, as provided in the examples, that has the following keys:
              - explanation: a string that explains why you think the input Data should get this resolution_score.
              - conversation_has_intent: true or false
              - agent_perceived_intent: a string that describes the intent the agent perceived from the user query
              - actual_user_intent: a string that describes the actual user intent
              - correct_intent_detected: true or false
              - intent_resolved: true or false
              - resolution_score: an integer between 1 and 5 that represents the resolution score


            # Output
            """;
#pragma warning restore S103 

        evaluationInstructions.Add(new ChatMessage(ChatRole.User, evaluationPrompt));

        return evaluationInstructions;
    }

    private static async ValueTask ParseEvaluationResponseAsync(
        NumericMetric metric,
        ChatResponse evaluationResponse,
        TimeSpan evaluationDuration,
        ChatConfiguration chatConfiguration,
        CancellationToken cancellationToken)
    {
        IntentResolutionRating rating;

        string evaluationResponseText = evaluationResponse.Text.Trim();
        if (string.IsNullOrEmpty(evaluationResponseText))
        {
            rating = IntentResolutionRating.Inconclusive;
            metric.AddDiagnostics(
                EvaluationDiagnostic.Error("The model failed to produce a valid evaluation response."));
        }
        else
        {
            try
            {
                rating = IntentResolutionRating.FromJson(evaluationResponseText);
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
                        rating = IntentResolutionRating.Inconclusive;
                        metric.AddDiagnostics(
                            EvaluationDiagnostic.Error(
                                $"""
                                Failed to repair the following response from the model and parse the score for '{IntentResolutionMetricName}':
                                {evaluationResponseText}
                                """));
                    }
                    else
                    {
                        rating = IntentResolutionRating.FromJson(repairedJson);
                    }
                }
                catch (JsonException ex)
                {
                    rating = IntentResolutionRating.Inconclusive;
                    metric.AddDiagnostics(
                        EvaluationDiagnostic.Error(
                            $"""
                            Failed to repair the following response from the model and parse the score for '{IntentResolutionMetricName}':
                            {evaluationResponseText}
                            {ex}
                            """));
                }
            }
        }

        UpdateMetric();

        void UpdateMetric()
        {
            metric.AddOrUpdateChatMetadata(evaluationResponse, evaluationDuration);
            metric.Value = rating.ResolutionScore;
            metric.Interpretation = metric.InterpretScore();
            metric.Reason = rating.Explanation;

            if (!string.IsNullOrWhiteSpace(rating.AgentPerceivedIntent))
            {
                metric.AddOrUpdateMetadata("agent_perceived_intent", rating.AgentPerceivedIntent!);
            }

            if (!string.IsNullOrWhiteSpace(rating.ActualUserIntent))
            {
                metric.AddOrUpdateMetadata("actual_user_intent", rating.ActualUserIntent!);
            }

            metric.AddOrUpdateMetadata("conversation_has_intent", rating.ConversationHasIntent.ToString());
            metric.AddOrUpdateMetadata("correct_intent_detected", rating.CorrectIntentDetected.ToString());
            metric.AddOrUpdateMetadata("intent_resolved", rating.IntentResolved.ToString());
        }
    }
}
