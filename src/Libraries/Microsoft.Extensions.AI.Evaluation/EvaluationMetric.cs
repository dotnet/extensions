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
/// <param name="reason">
/// An optional string that can be used to provide some commentary around the result represented by this
/// <see cref="EvaluationMetric"/>.
/// </param>
[JsonDerivedType(typeof(NumericMetric), "numeric")]
[JsonDerivedType(typeof(BooleanMetric), "boolean")]
[JsonDerivedType(typeof(StringMetric), "string")]
[JsonDerivedType(typeof(EvaluationMetric), "none")]
public class EvaluationMetric(string name, string? reason = null)
{
    /// <summary>
    /// Gets or sets the name of the <see cref="EvaluationMetric"/>.
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// Gets or sets a string that can optionally be used to provide some commentary around the result represented by
    /// this <see cref="EvaluationMetric"/>.
    /// </summary>
    public string? Reason { get; set; } = reason;

    /// <summary>
    /// Gets or sets an <see cref="EvaluationMetricInterpretation"/> that identifies whether the result of the
    /// evaluation represented by the current <see cref="EvaluationMetric"/> is considered good or bad, passed or
    /// failed etc.
    /// </summary>
    public EvaluationMetricInterpretation? Interpretation { get; set; }

#pragma warning disable CA2227
    /// <summary>
    /// Gets or sets any contextual information that was considered by the <see cref="IEvaluator"/> as part of the
    /// evaluation that produced the current <see cref="EvaluationMetric"/>.
    /// </summary>
    /// <remarks>
    /// Each entry in the returned dictionary has a name (key), and a collection of <see cref="AIContent"/> objects
    /// (value). An <see cref="IEvaluator"/> can use this dictionary to record one or more
    /// <see cref="EvaluationContext"/>s that it considred as part of the evaluation that produced this
    /// <see cref="EvaluationMetric"/>. For example, it can do so by including an entry with a name for the considered
    /// <see cref="EvaluationContext"/> as the key, and the <see cref="AIContent"/> objects returned from
    /// <see cref="EvaluationContext.GetContents"/> as the value.
    /// </remarks>
    public IDictionary<string, IList<AIContent>>? Context { get; set; }

    // CA2227: Collection properties should be read only.
    // We disable this warning because we want this type to be fully mutable for serialization purposes and for general
    // convenience.

    /// <summary>
    /// Gets or sets a collection of zero or more <see cref="EvaluationDiagnostic"/>s associated with the current
    /// <see cref="EvaluationMetric"/>.
    /// </summary>
    public IList<EvaluationDiagnostic>? Diagnostics { get; set; }

    /// <summary>
    /// Gets or sets a collection of zero or more string metadata associated with the current
    /// <see cref="EvaluationMetric"/>.
    /// </summary>
    public IDictionary<string, string>? Metadata { get; set; }
#pragma warning restore CA2227
}
