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
/// An <see cref="IEvaluator"/> that evaluates an AI system's effectiveness at using the tools supplied to it.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ToolCallAccuracyEvaluator"/> measures how accurately an AI system uses tools by examining tool calls
/// (i.e., <see cref="FunctionCallContent"/>s) present in the supplied response to assess the relevance of these tool
/// calls to the conversation, the parameter correctness for these tool calls with regard to the tool definitions
/// supplied via <see cref="ToolCallAccuracyEvaluatorContext.ToolDefinitions"/>, and the accuracy of the parameter
/// value extraction from the supplied conversation.
/// </para>
/// <para>
/// Note that at the moment, <see cref="ToolCallAccuracyEvaluator"/> only supports evaluating calls to tools that are
/// defined as <see cref="AIFunction"/>s. Any other <see cref="AITool"/> definitions that are supplied via
/// <see cref="ToolCallAccuracyEvaluatorContext.ToolDefinitions"/> will be ignored.
/// </para>
/// <para>
/// <see cref="ToolCallAccuracyEvaluator"/> returns a <see cref="BooleanMetric"/> that contains a score for 'Tool Call
/// Accuracy'. The score is <see langword="false"/> if the tool call is irrelevant or contains information not present
/// in the conversation and <see langword="true" /> if the tool call is relevant with properly extracted parameters
/// from the conversation.
/// </para>
/// <para>
/// <b>Note:</b> <see cref="ToolCallAccuracyEvaluator"/> is an AI-based evaluator that uses an AI model to perform its
/// evaluation. While the prompt that this evaluator uses to perform its evaluation is designed to be model-agnostic,
/// the performance of this prompt (and the resulting evaluation) can vary depending on the model used, and can be
/// especially poor when a smaller / local model is used.
/// </para>
/// <para>
/// The prompt that <see cref="ToolCallAccuracyEvaluator"/> uses has been tested against (and tuned to work well with)
/// the following models. So, using this evaluator with a model from the following list is likely to produce the best
/// results. (The model to be used can be configured via <see cref="ChatConfiguration.ChatClient"/>.)
/// </para>
/// <para>
/// <b>GPT-4o</b>
/// </para>
/// </remarks>
[Experimental("AIEVAL001")]
public sealed class ToolCallAccuracyEvaluator : IEvaluator
{
    /// <summary>
    /// Gets the <see cref="EvaluationMetric.Name"/> of the <see cref="BooleanMetric"/> returned by
    /// <see cref="ToolCallAccuracyEvaluator"/>.
    /// </summary>
    public static string ToolCallAccuracyMetricName => "Tool Call Accuracy";

    /// <inheritdoc/>
    public IReadOnlyCollection<string> EvaluationMetricNames { get; } = [ToolCallAccuracyMetricName];

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

        var metric = new BooleanMetric(ToolCallAccuracyMetricName);
        var result = new EvaluationResult(metric);

        if (!messages.Any())
        {
            metric.AddDiagnostics(
                EvaluationDiagnostic.Error(
                    "The conversation history supplied for evaluation did not include any messages."));

            return result;
        }

        IEnumerable<FunctionCallContent> toolCalls =
            modelResponse.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>();

        if (!toolCalls.Any())
        {
            metric.AddDiagnostics(
                EvaluationDiagnostic.Error($"The {nameof(modelResponse)} supplied for evaluation did not contain any tool calls (i.e., {nameof(FunctionCallContent)}s)."));

            return result;
        }

        if (additionalContext?.OfType<ToolCallAccuracyEvaluatorContext>().FirstOrDefault()
                is not ToolCallAccuracyEvaluatorContext context)
        {
            metric.AddDiagnostics(
                EvaluationDiagnostic.Error(
                    $"A value of type {nameof(ToolCallAccuracyEvaluatorContext)} was not found in the {nameof(additionalContext)} collection."));

            return result;
        }

        if (context.ToolDefinitions.Count is 0)
        {
            metric.AddDiagnostics(
                EvaluationDiagnostic.Error(
                    $"Supplied {nameof(ToolCallAccuracyEvaluatorContext)} did not contain any {nameof(ToolCallAccuracyEvaluatorContext.ToolDefinitions)}."));

            return result;
        }

        var toolDefinitionNames = new HashSet<string>(context.ToolDefinitions.Select(td => td.Name));

        if (toolCalls.Any(t => !toolDefinitionNames.Contains(t.Name)))
        {
            metric.AddDiagnostics(
                EvaluationDiagnostic.Error(
                    $"The {nameof(modelResponse)} supplied for evaluation contained calls to tools that were not included in the supplied {nameof(ToolCallAccuracyEvaluatorContext)}."));

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
        metric.AddOrUpdateContext(context);
        metric.Interpretation = metric.InterpretScore();
        return result;
    }

    private static List<ChatMessage> GetEvaluationInstructions(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ToolCallAccuracyEvaluatorContext context)
    {
#pragma warning disable S103 // Lines should not be too long
        const string SystemPrompt =
            """
            # Instruction
            ## Goal
            ### You are an expert in evaluating the accuracy of a tool call considering relevance and potential usefulness including syntactic and semantic correctness of a proposed tool call from an intelligent system based on provided definition and data. Your goal will involve answering the questions below using the information provided.
            - **Definition**: You are given a definition of the communication trait that is being evaluated to help guide your Score.
            - **Data**: Your input data include CONVERSATION , TOOL CALL and TOOL DEFINITION.
            - **Tasks**: To complete your evaluation you will be asked to evaluate the Data in different ways.
            """;
#pragma warning restore S103

        List<ChatMessage> evaluationInstructions = [new ChatMessage(ChatRole.System, SystemPrompt)];

        string renderedConversation = messages.RenderText();
        string renderedToolCallsAndResults = modelResponse.RenderToolCallsAndResultsAsJson();
        string renderedToolDefinitions = context.ToolDefinitions.RenderAsJson();

#pragma warning disable S103 // Lines should not be too long
        string evaluationPrompt =
            $$"""
            # Definition
            **Tool Call Accuracy** refers to the relevance and potential usefulness of a TOOL CALL in the context of an ongoing CONVERSATION and EXTRACTION of RIGHT PARAMETER VALUES from the CONVERSATION.It assesses how likely the TOOL CALL is to contribute meaningfully to the CONVERSATION and help address the user's needs. Focus on evaluating the potential value of the TOOL CALL within the specific context of the given CONVERSATION, without making assumptions beyond the provided information.
              Consider the following factors in your evaluation:

              1. Relevance: How well does the proposed tool call align with the current topic and flow of the conversation?
              2. Parameter Appropriateness: Do the parameters used in the TOOL CALL match the TOOL DEFINITION and are the parameters relevant to the latest user's query?
              3. Parameter Value Correctness: Are the parameters values used in the TOOL CALL present or inferred by CONVERSATION and relevant to the latest user's query?
              4. Potential Value: Is the information this tool call might provide likely to be useful in advancing the conversation or addressing the user expressed or implied needs?
              5. Context Appropriateness: Does the tool call make sense at this point in the conversation, given what has been discussed so far?


            # Ratings
            ## [Tool Call Accuracy: 0] (Irrelevant)
            **Definition:**
             1. The TOOL CALL is not relevant and will not help resolve the user's need.
             2. TOOL CALL include parameters values that are not present or inferred from CONVERSATION.
             3. TOOL CALL has parameters that is not present in TOOL DEFINITION.

            ## [Tool Call Accuracy: 1] (Relevant)
            **Definition:**
             1. The TOOL CALL is directly relevant and very likely to help resolve the user's need.
             2. TOOL CALL include parameters values that are present or inferred from CONVERSATION.
             3. TOOL CALL has parameters that is present in TOOL DEFINITION.

            # Data
            CONVERSATION : {{renderedConversation}}
            TOOL CALL: {{renderedToolCallsAndResults}}
            TOOL DEFINITION: {{renderedToolDefinitions}}


            # Tasks
            ## Please provide your assessment Score for the previous CONVERSATION , TOOL CALL and TOOL DEFINITION based on the Definitions above. Your output should include the following information:
            - **ThoughtChain**: To improve the reasoning process, think step by step and include a step-by-step explanation of your thought process as you analyze the data based on the definitions. Keep it brief and start your ThoughtChain with "Let's think step by step:".
            - **Explanation**: a very short explanation of why you think the input Data should get that Score.
            - **Score**: based on your previous analysis, provide your Score. The Score you give MUST be a integer score (i.e., "0", "1") based on the levels of the definitions.


            ## Please provide your answers between the tags: <S0>your chain of thoughts</S0>, <S1>your explanation</S1>, <S2>your Score</S2>.
            # Output
            """;
#pragma warning restore S103 

        evaluationInstructions.Add(new ChatMessage(ChatRole.User, evaluationPrompt));

        return evaluationInstructions;
    }
}
