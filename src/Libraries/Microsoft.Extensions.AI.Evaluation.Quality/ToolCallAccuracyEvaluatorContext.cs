// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AI.Evaluation.Quality;

/// <summary>
/// Contextual information that the <see cref="ToolCallAccuracyEvaluator"/> uses to evaluate an AI system's
/// effectiveness at using the tools supplied to it.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ToolCallAccuracyEvaluator"/> measures how accurately an AI system uses tools by examining tool calls
/// (i.e., <see cref="FunctionCallContent"/>s) present in the supplied response to assess the relevance of these tool
/// calls to the conversation, the parameter correctness for these tool calls with regard to the tool definitions
/// supplied via <see cref="ToolDefinitions"/>, and the accuracy of the parameter value extraction from the supplied
/// conversation history.
/// </para>
/// <para>
/// Note that at the moment, <see cref="ToolCallAccuracyEvaluator"/> only supports evaluating calls to tools that are
/// defined as <see cref="AIFunction"/>s. Any other <see cref="AITool"/> definitions that are supplied via
/// <see cref="ToolDefinitions"/> will be ignored.
/// </para>
/// </remarks>
[Experimental("AIEVAL001")]
public sealed class ToolCallAccuracyEvaluatorContext : EvaluationContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToolCallAccuracyEvaluatorContext"/> class.
    /// </summary>
    /// <param name="toolDefinitions">
    /// <para>
    /// The set of tool definitions (see <see cref="ChatOptions.Tools"/>) that were used when generating the model
    /// response that is being evaluated.
    /// </para>
    /// <para>
    /// Note that at the moment, <see cref="ToolCallAccuracyEvaluator"/> only supports evaluating calls to tools that
    /// are defined as <see cref="AIFunction"/>s. Any other <see cref="AITool"/> definitions will be ignored.
    /// </para>
    /// </param>
    public ToolCallAccuracyEvaluatorContext(IEnumerable<AITool> toolDefinitions)
        : base(name: ToolCallAccuracyContextName, contents: [new TextContent(toolDefinitions.RenderAsJson())])
    {
        ToolDefinitions = [.. toolDefinitions];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolCallAccuracyEvaluatorContext"/> class.
    /// </summary>
    /// <param name="toolDefinitions">
    /// <para>
    /// The set of tool definitions (see <see cref="ChatOptions.Tools"/>) that were used when generating the model
    /// response that is being evaluated.
    /// </para>
    /// <para>
    /// Note that at the moment, <see cref="ToolCallAccuracyEvaluator"/> only supports evaluating calls to tools that
    /// are defined as <see cref="AIFunction"/>s. Any other <see cref="AITool"/> definitions will be ignored.
    /// </para>
    /// </param>
    public ToolCallAccuracyEvaluatorContext(params AITool[] toolDefinitions)
        : this(toolDefinitions as IEnumerable<AITool>)
    {
    }

    /// <summary>
    /// Gets the unique <see cref="EvaluationContext.Name"/> that is used for
    /// <see cref="ToolCallAccuracyEvaluatorContext"/>.
    /// </summary>
    public static string ToolCallAccuracyContextName => "Tool Definitions (Tool Call Accuracy)";

    /// <summary>
    /// Gets set of tool definitions (see <see cref="ChatOptions.Tools"/>) that were used when generating the model
    /// response that is being evaluated.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="ToolCallAccuracyEvaluator"/> measures how accurately an AI system uses tools by examining tool calls
    /// (i.e., <see cref="FunctionCallContent"/>s) present in the supplied response to assess the relevance of these
    /// tool calls to the conversation, the parameter correctness for these tool calls with regard to the tool
    /// definitions supplied via <see cref="ToolDefinitions"/>, and the accuracy of the parameter value extraction from
    /// the supplied conversation history.
    /// </para>
    /// <para>
    /// Note that at the moment, <see cref="ToolCallAccuracyEvaluator"/> only supports evaluating calls to tools that
    /// are defined as <see cref="AIFunction"/>s. Any other <see cref="AITool"/> definitions that are supplied via
    /// <see cref="ToolDefinitions"/> will be ignored.
    /// </para>
    /// </remarks>
    public IReadOnlyList<AITool> ToolDefinitions { get; }
}
