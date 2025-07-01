// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.AI.Evaluation.NLP;

/// <summary>
/// Contextual information that the <see cref="BLEUEvaluator"/> uses to compute the BLEU score for a response.
/// </summary>
/// <remarks>
/// <see cref="BLEUEvaluator"/> measures the BLEU score of a response compared to a reference. BLEU (Bilingual Evaluation Understudy)
/// is a metric used to evaluate the quality of machine-generated text.
/// </remarks>
public sealed class BLEUEvaluatorContext : EvaluationContext
{
    /// <summary>
    /// Gets the unique <see cref="EvaluationContext.Name"/> that is used for
    /// <see cref="BLEUEvaluatorContext"/>.
    /// </summary>
    public static string ReferencesContextName => "References (BLEU)";

    /// <summary>
    /// Gets the references against which the provided response will be scored.
    /// </summary>
    /// <remarks>
    /// The <see cref="BLEUEvaluator"/> measures the degree to which the response being evaluated is similar to
    /// the response supplied via <see cref="References"/>. The metric will be reported as a BLEU score.
    /// </remarks>
    public IReadOnlyList<string> References { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BLEUEvaluatorContext"/> class.
    /// </summary>
    /// <param name="references">
    /// The reference responses against which the response that is being evaluated is compared.
    /// </param>
    public BLEUEvaluatorContext(IEnumerable<string> references)
        : this(references.ToArray())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BLEUEvaluatorContext"/> class.
    /// </summary>
    /// <param name="references">
    /// The reference responses against which the response that is being evaluated is compared.
    /// </param>
    public BLEUEvaluatorContext(params string[] references)
        : base(
            name: ReferencesContextName,
            contents: [.. references.Select(c => new TextContent(c))])
    {
        References = references;
    }
}
