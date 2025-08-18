// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

namespace Microsoft.Extensions.AI.Evaluation.Safety;

/// <summary>
/// Contextual information that the <see cref="UngroundedAttributesEvaluator"/> uses to evaluate whether a response is
/// ungrounded.
/// </summary>
/// <param name="groundingContext">
/// Contextual information against which the groundedness (or ungroundedness) of a response is evaluated.
/// </param>
/// <remarks>
/// <see cref="UngroundedAttributesEvaluator"/> measures whether the response being evaluated is first, ungrounded
/// based on the information present in the supplied <paramref name="groundingContext"/>. It then checks whether the
/// response contains information about the protected class or emotional state of a person.
/// </remarks>
public sealed class UngroundedAttributesEvaluatorContext(string groundingContext)
    : EvaluationContext(name: GroundingContextName, content: groundingContext)
{
    /// <summary>
    /// Gets the unique <see cref="EvaluationContext.Name"/> that is used for
    /// <see cref="UngroundedAttributesEvaluatorContext"/>.
    /// </summary>
    public static string GroundingContextName => "Grounding Context (Ungrounded Attributes)";

    /// <summary>
    /// Gets the contextual information against which the groundedness (or ungroundedness) of a response is evaluated.
    /// </summary>
    /// <remarks>
    /// The <see cref="UngroundedAttributesEvaluator"/> measures whether the response being evaluated is first,
    /// ungrounded based on the information present in the supplied <see cref="GroundingContext"/>. It then checks
    /// whether the response contains information about the protected class or emotional state of a person.
    /// </remarks>
    public string GroundingContext { get; } = groundingContext;
}
