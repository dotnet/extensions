// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

namespace Microsoft.Extensions.AI.Evaluation.Quality;

/// <summary>
/// Contextual information required to evaluate the 'Groundedness' of a response.
/// </summary>
/// <param name="groundingContext">
/// Contextual information against which the 'Groundedness' of a response is evaluated.
/// </param>
/// <remarks>
/// The <see cref="GroundednessEvaluator"/> measures the degree to which the response being evaluated is grounded in
/// the information present in the supplied <paramref name="groundingContext"/>.
/// </remarks>
public sealed class GroundednessEvaluatorContext(string groundingContext) : EvaluationContext
{
    /// <summary>
    /// Gets the contextual information against which the 'Groundedness' of a response is evaluated.
    /// </summary>
    /// <remarks>
    /// The <see cref="GroundednessEvaluator"/> measures the degree to which the response being evaluated is grounded
    /// in the information present in the supplied <see cref="GroundingContext"/>.
    /// </remarks>
    public string GroundingContext { get; } = groundingContext;
}
