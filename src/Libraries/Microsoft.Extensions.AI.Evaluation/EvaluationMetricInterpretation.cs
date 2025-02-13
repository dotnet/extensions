// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

namespace Microsoft.Extensions.AI.Evaluation;

/// <summary>
/// Specifies how the result represented in an associated <see cref="EvaluationMetric"/> should be interpreted.
/// </summary>
/// <param name="rating">
/// An <see cref="EvaluationRating"/> that identifies how good or bad the result represented in the associated
/// <see cref="EvaluationMetric"/> is considered.
/// </param>
/// <param name="failed">
/// <see langword="true"/> if the result represented in the associated <see cref="EvaluationMetric"/> is considered a
/// failure; <see langword="false"/> otherwise.
/// </param>
/// <param name="reason">
/// An optional string that can be used to provide some commentary around the values specified for
/// <paramref name="rating"/> and / or <paramref name="failed"/>.
/// </param>
public sealed class EvaluationMetricInterpretation(
    EvaluationRating rating = EvaluationRating.Unknown,
    bool failed = false,
    string? reason = null)
{
    /// <summary>
    /// Gets or sets an <see cref="EvaluationRating"/> that identifies how good or bad the result represented in the
    /// associated <see cref="EvaluationMetric"/> is considered.
    /// </summary>
    public EvaluationRating Rating { get; set; } = rating;

    /// <summary>
    /// Gets or sets a value indicating whether the result represented in the associated <see cref="EvaluationMetric"/>
    /// is considered a failure.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the result represented in the associated <see cref="EvaluationMetric"/> is considered
    /// a failure; <see langword="false"/> otherwise.
    /// </value>
    public bool Failed { get; set; } = failed;

    /// <summary>
    /// Gets or sets a string that can optionally be used to provide some commentary around the values specified for
    /// <see cref="Rating"/> and / or <see cref="Failed"/>.
    /// </summary>
    public string? Reason { get; set; } = reason;
}
