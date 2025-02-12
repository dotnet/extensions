// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

namespace Microsoft.Extensions.AI.Evaluation.Quality;

/// <summary>
/// Options for <see cref="RelevanceTruthAndCompletenessEvaluator"/>.
/// </summary>
/// <param name="includeReasoning">
/// If <paramref name="includeReasoning"/> is set to <see langword="true"/>, this instructs the
/// <see cref="RelevanceTruthAndCompletenessEvaluator"/> to include <see cref="EvaluationDiagnostic"/>s (with
/// <see cref="EvaluationDiagnostic.Severity"/> set to <see cref="EvaluationDiagnosticSeverity.Informational"/>) as
/// part of the returned <see cref="NumericMetric"/>s for 'Relevance' 'Truth' and 'Completeness' that explain the
/// reasoning behind the corresponding scores. By default, <paramref name="includeReasoning"/> is set to
/// <see langword="false"/>.
/// </param>
public sealed class RelevanceTruthAndCompletenessEvaluatorOptions(bool includeReasoning = false)
{
    /// <summary>
    /// Gets the default options for <see cref="RelevanceTruthAndCompletenessEvaluator"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="IncludeReasoning"/> is set to <see langword="false"/> by default.
    /// </remarks>
    public static RelevanceTruthAndCompletenessEvaluatorOptions Default { get; } =
        new RelevanceTruthAndCompletenessEvaluatorOptions();

    /// <summary>
    /// Gets a value indicating whether the <see cref="RelevanceTruthAndCompletenessEvaluator"/> should include
    /// <see cref="EvaluationDiagnostic"/>s (with <see cref="EvaluationDiagnostic.Severity"/> set to
    /// <see cref="EvaluationDiagnosticSeverity.Informational"/>) as part of the returned
    /// <see cref="NumericMetric"/>s for 'Relevance' 'Truth' and 'Completeness' to explain the reasoning behind the
    /// corresponding scores. By default, <see cref="IncludeReasoning"/> is set to <see langword="false"/>.
    /// </summary>
    public bool IncludeReasoning { get; } = includeReasoning;
}
