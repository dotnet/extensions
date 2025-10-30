// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.AI.Evaluation.NLP;

/// <summary>
/// Contextual information that the <see cref="GLEUEvaluator"/> uses to compute the GLEU score for a response.
/// </summary>
/// <remarks>
/// <see cref="GLEUEvaluator"/> measures the GLEU score of a response compared to one or more reference responses
/// supplied via <see cref="References"/>. GLEU (Google-BLEU) is a metric used to evaluate the quality of
/// machine-generated text.
/// </remarks>
public sealed class GLEUEvaluatorContext : EvaluationContext
{
    /// <summary>
    /// Gets the unique <see cref="EvaluationContext.Name"/> that is used for
    /// <see cref="GLEUEvaluatorContext"/>.
    /// </summary>
    public static string ReferencesContextName => "References (GLEU)";

    /// <summary>
    /// Gets the references against which the provided response will be scored.
    /// </summary>
    /// <remarks>
    /// The <see cref="GLEUEvaluator"/> measures the degree to which the response being evaluated is similar to
    /// the responses supplied via <see cref="References"/>. The metric will be reported as a GLEU score.
    /// </remarks>
    public IReadOnlyList<string> References { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GLEUEvaluatorContext"/> class.
    /// </summary>
    /// <param name="references">
    /// The reference responses against which the response that is being evaluated is compared.
    /// </param>
    public GLEUEvaluatorContext(IEnumerable<string> references)
        : this(references.ToArray())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GLEUEvaluatorContext"/> class.
    /// </summary>
    /// <param name="references">
    /// The reference responses against which the response that is being evaluated is compared.
    /// </param>
    public GLEUEvaluatorContext(params string[] references)
        : base(
            name: ReferencesContextName,
            contents: [.. references.Select(c => new TextContent(c))])
    {
        References = references;
    }
}
