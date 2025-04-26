// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

namespace Microsoft.Extensions.AI.Evaluation.Quality;

/// <summary>
/// Contextual information that the <see cref="CompletenessEvaluator"/> uses to evaluate the 'Completeness' of a
/// response.
/// </summary>
/// <param name="groundTruth">
/// The ground truth against which the response that is being evaluated is assessed.
/// </param>
/// <remarks>
/// <see cref="CompletenessEvaluator"/> measures an AI system's ability to deliver comprehensive and accurate
/// responses. It assesses how thoroughly the response aligns with the key information, claims, and statements
/// established in the supplied <paramref name="groundTruth"/>.
/// </remarks>
public sealed class CompletenessEvaluatorContext(string groundTruth)
    : EvaluationContext(name: GroundTruthContextName, content: groundTruth)
{
    /// <summary>
    /// Gets the unique <see cref="EvaluationContext.Name"/> that is used for
    /// <see cref="CompletenessEvaluatorContext"/>.
    /// </summary>
    public static string GroundTruthContextName => "Ground Truth (Completeness)";

    /// <summary>
    /// Gets the ground truth against which the response that is being evaluated is assessed.
    /// </summary>
    /// <remarks>
    /// <see cref="CompletenessEvaluator"/> measures an AI system's ability to deliver comprehensive and accurate
    /// responses. It assesses how thoroughly the response aligns with the key information, claims, and statements
    /// established in the supplied <see cref="GroundTruth"/>.
    /// </remarks>
    public string GroundTruth { get; } = groundTruth;
}
