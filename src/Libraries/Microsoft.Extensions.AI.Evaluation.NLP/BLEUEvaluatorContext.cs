// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

namespace Microsoft.Extensions.AI.Evaluation.NLP;

/// <summary>
/// Contextual information that the <see cref="BLEUEvaluator"/> uses to compute the BLEU score for a response.
/// </summary>
/// <param name="reference">
/// The reference response against which the response that is being evaluated is compared.
/// </param>
/// <remarks>
/// <see cref="BLEUEvaluator"/> measures the BLEU score of a response compared to a reference. BLEU (Bilingual Evaluation Understudy)
/// is a metric used to evaluate the quality if machine-generated text.
/// </remarks>
public sealed class BLEUEvaluatorContext(string reference)
    : EvaluationContext(name: BLEUContext, content: reference)
{
    /// <summary>
    /// Gets the unique <see cref="EvaluationContext.Name"/> that is used for
    /// <see cref="BLEUEvaluatorContext"/>.
    /// </summary>
    public static string BLEUContext => "BLEU Context";

    /// <summary>
    /// Gets the reference response against which the provided chat response will be scored.
    /// </summary>
    /// <remarks>
    /// The <see cref="BLEUEvaluator"/> measures the degree to which the response being evaluated is similar to
    /// the response supplied via <see cref="ReferenceText"/>. The metric will be reported as a BLEU score.
    /// </remarks>
    public string ReferenceText { get; } = reference;
}
