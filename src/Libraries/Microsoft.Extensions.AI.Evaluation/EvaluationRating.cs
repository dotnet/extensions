// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI.Evaluation;

/// <summary>
/// An enumeration that identifies the set of possible ways in which an <see cref="EvaluationMetric"/> can be
/// interpreted.
/// </summary>
public enum EvaluationRating
{
    /// <summary>
    /// A value that indicates that the <see cref="EvaluationMetric{T}.Value"/> is unknown.
    /// </summary>
    Unknown,

    /// <summary>
    /// A value that indicates that the <see cref="EvaluationMetric{T}.Value"/> cannot be interpreted conclusively.
    /// </summary>
    Inconclusive,

    /// <summary>
    /// A value that indicates that the <see cref="EvaluationMetric{T}.Value"/> is interpreted as being exceptional.
    /// </summary>
    Exceptional,

    /// <summary>
    /// A value that indicates that the <see cref="EvaluationMetric{T}.Value"/> is interpreted as being good.
    /// </summary>
    Good,

    /// <summary>
    /// A value that indicates that the <see cref="EvaluationMetric{T}.Value"/> is interpreted as being average.
    /// </summary>
    Average,

    /// <summary>
    /// A value that indicates that the <see cref="EvaluationMetric{T}.Value"/> is interpreted as being poor.
    /// </summary>
    Poor,

    /// <summary>
    /// A value that indicates that the <see cref="EvaluationMetric{T}.Value"/> is interpreted as being unacceptable.
    /// </summary>
    Unacceptable,
}
