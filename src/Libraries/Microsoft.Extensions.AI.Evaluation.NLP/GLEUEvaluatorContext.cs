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
/// Contextual information that the <see cref="GLEUEvaluator"/> uses to compute the GLEU score for a response.
/// </summary>
/// <remarks>
/// <see cref="GLEUEvaluator"/> measures the GLEU score of a response compared to a reference. GLEU (Google-BLEU)
/// is a metric used to evaluate the quality of machine-generated text.
/// </remarks>
public sealed class GLEUEvaluatorContext : EvaluationContext
{
    /// <summary>
    /// Gets the unique <see cref="EvaluationContext.Name"/> that is used for
    /// <see cref="GLEUEvaluatorContext"/>.
    /// </summary>
    public static string GLEUContextName => "GLEU Context";

    /// <summary>
    /// Gets the reference response against which the provided chat response will be scored.
    /// </summary>
    /// <remarks>
    /// The <see cref="GLEUEvaluator"/> measures the degree to which the response being evaluated is similar to
    /// the response supplied via <see cref="References"/>. The metric will be reported as a GLEU score.
    /// </remarks>
    public IReadOnlyList<string> References { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GLEUEvaluatorContext"/> class.
    /// </summary>
    /// <param name="references">
    /// The reference responses against which the response that is being evaluated is compared.
    /// </param>
    public GLEUEvaluatorContext(params string[] references)
        : this(references as IEnumerable<string>)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GLEUEvaluatorContext"/> class.
    /// </summary>
    /// <param name="references">
    /// The reference responses against which the response that is being evaluated is compared.
    /// </param>
    public GLEUEvaluatorContext(IEnumerable<string> references)
        : base(
            name: GLEUContextName,
            contents: [.. references.Select(c => new TextContent(c))])
    {
        References = [.. references];
    }
}
