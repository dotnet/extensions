// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

namespace Microsoft.Extensions.AI.Evaluation.NLP;

/// <summary>
/// Contextual information that the <see cref="F1Evaluator"/> uses to compute the F1 score for a response.
/// </summary>
/// <remarks>
/// <see cref="F1Evaluator"/> measures the F1 score of a response compared to a reference. F1 (Google-BLEU)
/// is a metric used to evaluate the quality of machine-generated text.
/// </remarks>
public sealed class F1EvaluatorContext : EvaluationContext
{
    /// <summary>
    /// Gets the unique <see cref="EvaluationContext.Name"/> that is used for
    /// <see cref="F1EvaluatorContext"/>.
    /// </summary>
    public static string F1ContextName => "F1 Context";

    /// <summary>
    /// Gets the reference response against which the provided chat response will be scored.
    /// </summary>
    /// <remarks>
    /// The <see cref="F1Evaluator"/> measures the degree to which the response being evaluated is similar to
    /// the response supplied via <see cref="GroundTruth"/>. The metric will be reported as a F1 score.
    /// </remarks>
    public string GroundTruth { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="F1EvaluatorContext"/> class.
    /// </summary>
    /// <param name="groundTruth">
    /// The reference response against which the response that is being evaluated is compared.
    /// </param>
    public F1EvaluatorContext(string groundTruth)
        : base(
            name: F1ContextName,
            contents: [new TextContent(groundTruth)])
    {
        GroundTruth = groundTruth;
    }
}
