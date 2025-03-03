// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

namespace Microsoft.Extensions.AI.Evaluation;

/// <summary>
/// An base class that represents the result of an evaluation containing a value of type
/// <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the <see cref="Value"/>.</typeparam>
public class EvaluationMetric<T> : EvaluationMetric
{
    /// <summary>
    /// Gets or sets the value of the <see cref="EvaluationMetric{T}"/>.
    /// </summary>
    public T? Value { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvaluationMetric{T}"/> class.
    /// </summary>
    /// <param name="name">The name of the <see cref="EvaluationMetric{T}"/>.</param>
    /// <param name="value">The value  of the <see cref="EvaluationMetric{T}"/>.</param>
    protected EvaluationMetric(string name, T? value)
        : base(name)
    {
        Value = value;
    }
}
