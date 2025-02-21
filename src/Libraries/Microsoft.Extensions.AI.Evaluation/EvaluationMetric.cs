// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI.Evaluation;

/// <summary>
/// A base class that represents the result of an evaluation.
/// </summary>
/// <param name="name">The name of the <see cref="EvaluationMetric"/>.</param>
[JsonDerivedType(typeof(NumericMetric), "numeric")]
[JsonDerivedType(typeof(BooleanMetric), "boolean")]
[JsonDerivedType(typeof(StringMetric), "string")]
[JsonDerivedType(typeof(EvaluationMetric), "none")]
public class EvaluationMetric(string name)
{
    /// <summary>
    /// Gets or sets the name of the <see cref="EvaluationMetric"/>.
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// Gets or sets an <see cref="EvaluationMetricInterpretation"/> that identifies whether the result of the
    /// evaluation represented by the current <see cref="EvaluationMetric"/> is considered good or bad, passed or
    /// failed etc.
    /// </summary>
    public EvaluationMetricInterpretation? Interpretation { get; set; }

    /// <summary>
    /// Gets or sets a collection of zero or more <see cref="EvaluationDiagnostic"/>s associated with the current
    /// <see cref="EvaluationMetric"/>.
    /// </summary>
#pragma warning disable CA2227
    // CA2227: Collection properties should be read only.
    // We disable this warning because we want this type to be fully mutable for serialization purposes and for general
    // convenience.
    public IList<EvaluationDiagnostic> Diagnostics { get; set; } = [];
#pragma warning restore CA2227

    /// <summary>
    /// Adds a <see cref="EvaluationDiagnostic"/> to the current <see cref="EvaluationMetric"/>'s
    /// <see cref="Diagnostics"/>.
    /// </summary>
    /// <param name="diagnostic">The <see cref="EvaluationDiagnostic"/> to be added.</param>
    public void AddDiagnostic(EvaluationDiagnostic diagnostic)
        => Diagnostics.Add(diagnostic);
}
