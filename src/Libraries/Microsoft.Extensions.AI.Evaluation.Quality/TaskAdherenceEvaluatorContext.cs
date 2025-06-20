// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AI.Evaluation.Quality;

/// <summary>
/// Contextual information that the <see cref="TaskAdherenceEvaluator"/> uses to evaluate an AI system's
/// effectiveness at adhering to the task assigned to it.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TaskAdherenceEvaluator"/> measures how accurately an AI system adheres to the task assigned to it by
/// examining the alignment of the supplied response with instructions and definitions present in the conversation
/// history, the accuracy and clarity of the response, and the proper use of tool definitions supplied via
/// <see cref="ToolDefinitions"/>.
/// </para>
/// <para>
/// Note that at the moment, <see cref="TaskAdherenceEvaluator"/> only supports evaluating calls to tools that are
/// defined as <see cref="AIFunction"/>s. Any other <see cref="AITool"/> definitions that are supplied via
/// <see cref="ToolDefinitions"/> will be ignored.
/// </para>
/// </remarks>
[Experimental("AIEVAL001")]
public sealed class TaskAdherenceEvaluatorContext : EvaluationContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TaskAdherenceEvaluatorContext"/> class.
    /// </summary>
    /// <param name="toolDefinitions">
    /// <para>
    /// The set of tool definitions (see <see cref="ChatOptions.Tools"/>) that were used when generating the model
    /// response that is being evaluated.
    /// </para>
    /// <para>
    /// Note that at the moment, <see cref="TaskAdherenceEvaluator"/> only supports evaluating calls to tools that
    /// are defined as <see cref="AIFunction"/>s. Any other <see cref="AITool"/> definitions will be ignored.
    /// </para>
    /// </param>
    public TaskAdherenceEvaluatorContext(IEnumerable<AITool> toolDefinitions)
        : base(name: TaskAdherenceContextName, contents: [new TextContent(toolDefinitions.RenderAsJson())])
    {
        ToolDefinitions = [.. toolDefinitions];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskAdherenceEvaluatorContext"/> class.
    /// </summary>
    /// <param name="toolDefinitions">
    /// <para>
    /// The set of tool definitions (see <see cref="ChatOptions.Tools"/>) that were used when generating the model
    /// response that is being evaluated.
    /// </para>
    /// <para>
    /// Note that at the moment, <see cref="TaskAdherenceEvaluator"/> only supports evaluating calls to tools that
    /// are defined as <see cref="AIFunction"/>s. Any other <see cref="AITool"/> definitions will be ignored.
    /// </para>
    /// </param>
    public TaskAdherenceEvaluatorContext(params AITool[] toolDefinitions)
        : this(toolDefinitions as IEnumerable<AITool>)
    {
    }

    /// <summary>
    /// Gets the unique <see cref="EvaluationContext.Name"/> that is used for
    /// <see cref="TaskAdherenceEvaluatorContext"/>.
    /// </summary>
    public static string TaskAdherenceContextName => "Tool Definitions (Task Adherence)";

    /// <summary>
    /// Gets set of tool definitions (see <see cref="ChatOptions.Tools"/>) that were used when generating the model
    /// response that is being evaluated.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="TaskAdherenceEvaluator"/> measures how accurately an AI system adheres to the task assigned to it by
    /// examining the alignment of the supplied response with instructions and definitions present in the conversation
    /// history, the accuracy and clarity of the response, and the proper use of tool definitions supplied via
    /// <see cref="ToolDefinitions"/>.
    /// </para>
    /// <para>
    /// Note that at the moment, <see cref="TaskAdherenceEvaluator"/> only supports evaluating calls to tools that are
    /// defined as <see cref="AIFunction"/>s. Any other <see cref="AITool"/> definitions that are supplied via
    /// <see cref="ToolDefinitions"/> will be ignored.
    /// </para>
    /// </remarks>
    public IReadOnlyList<AITool> ToolDefinitions { get; }
}
