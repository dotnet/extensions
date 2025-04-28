﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

namespace Microsoft.Extensions.AI.Evaluation.Quality;

/// <summary>
/// Contextual information that the <see cref="EquivalenceEvaluator"/> uses to evaluate the 'Equivalence' of a
/// response.
/// </summary>
/// <param name="groundTruth">
/// The ground truth response against which the response that is being evaluated is compared.
/// </param>
/// <remarks>
/// <see cref="EquivalenceEvaluator"/> measures the degree to which the response being evaluated is similar to the
/// response supplied via <paramref name="groundTruth"/>.
/// </remarks>
public sealed class EquivalenceEvaluatorContext(string groundTruth)
    : EvaluationContext(name: GroundTruthContextName, content: groundTruth)
{
    /// <summary>
    /// Gets the unique <see cref="EvaluationContext.Name"/> that is used for
    /// <see cref="EquivalenceEvaluatorContext"/>.
    /// </summary>
    public static string GroundTruthContextName => "Ground Truth (Equivalence)";

    /// <summary>
    /// Gets the ground truth response against which the response that is being evaluated is compared.
    /// </summary>
    /// <remarks>
    /// The <see cref="EquivalenceEvaluator"/> measures the degree to which the response being evaluated is similar to
    /// the response supplied via <see cref="GroundTruth"/>.
    /// </remarks>
    public string GroundTruth { get; } = groundTruth;
}
