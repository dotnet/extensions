// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AI.Evaluation.Quality;

/// <summary>
/// Contextual information that the <see cref="IntentResolutionEvaluator"/> uses to evaluate an AI system's
/// effectiveness at identifying and resolving user intent.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IntentResolutionEvaluator"/> evaluates an AI system's effectiveness at identifying and resolving user
/// intent based on the supplied conversation history and the tool definitions supplied via
/// <see cref="ToolDefinitions"/>.
/// </para>
/// <para>
/// Note that at the moment, <see cref="IntentResolutionEvaluator"/> only supports evaluating calls to tools that are
/// defined as <see cref="AIFunction"/>s. Any other <see cref="AITool"/> definitions that are supplied via
/// <see cref="ToolDefinitions"/> will be ignored.
/// </para>
/// </remarks>
[Experimental("AIEVAL001")]
public sealed class IntentResolutionEvaluatorContext : EvaluationContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IntentResolutionEvaluatorContext"/> class.
    /// </summary>
    /// <param name="toolDefinitions">
    /// <para>
    /// The set of tool definitions (see <see cref="ChatOptions.Tools"/>) that were used when generating the model
    /// response that is being evaluated.
    /// </para>
    /// <para>
    /// Note that at the moment, <see cref="IntentResolutionEvaluator"/> only supports evaluating calls to tools that
    /// are defined as <see cref="AIFunction"/>s. Any other <see cref="AITool"/> definitions will be ignored.
    /// </para>
    /// </param>
    public IntentResolutionEvaluatorContext(IEnumerable<AITool> toolDefinitions)
        : base(name: IntentResolutionContextName, contents: [new TextContent(toolDefinitions.RenderAsJson())])
    {
        ToolDefinitions = [.. toolDefinitions];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IntentResolutionEvaluatorContext"/> class.
    /// </summary>
    /// <param name="toolDefinitions">
    /// <para>
    /// The set of tool definitions (see <see cref="ChatOptions.Tools"/>) that were used when generating the model
    /// response that is being evaluated.
    /// </para>
    /// <para>
    /// Note that at the moment, <see cref="IntentResolutionEvaluator"/> only supports evaluating calls to tools that
    /// are defined as <see cref="AIFunction"/>s. Any other <see cref="AITool"/> definitions will be ignored.
    /// </para>
    /// </param>
    public IntentResolutionEvaluatorContext(params AITool[] toolDefinitions)
        : this(toolDefinitions as IEnumerable<AITool>)
    {
    }

    /// <summary>
    /// Gets the unique <see cref="EvaluationContext.Name"/> that is used for
    /// <see cref="IntentResolutionEvaluatorContext"/>.
    /// </summary>
    public static string IntentResolutionContextName => "Tool Definitions (Intent Resolution)";

    /// <summary>
    /// Gets set of tool definitions (see <see cref="ChatOptions.Tools"/>) that were used when generating the model
    /// response that is being evaluated.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="IntentResolutionEvaluator"/> evaluates an AI system's effectiveness at identifying and resolving user
    /// intent based on the supplied conversation history and the tool definitions supplied via
    /// <see cref="ToolDefinitions"/>.
    /// </para>
    /// <para>
    /// Note that at the moment, <see cref="IntentResolutionEvaluator"/> only supports evaluating calls to tools that
    /// are defined as <see cref="AIFunction"/>s. Any other <see cref="AITool"/> definitions that are supplied via
    /// <see cref="ToolDefinitions"/> will be ignored.
    /// </para>
    /// </remarks>
    public IReadOnlyList<AITool> ToolDefinitions { get; }
}
