// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI.Evaluation;

/// <summary>
/// An <see cref="EvaluationMetric"/> containing a numeric value.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="NumericMetric"/> can be used to represent any numeric value. The underlying type of a
/// <see cref="NumericMetric"/>'s value is <see langword="double"/>. However, it can be used to represent any type of
/// numeric value including <see langword="int"/>, <see langword="long"/>, <see langword="float"/> etc.
/// </para>
/// <para>
/// A common use case for <see cref="NumericMetric"/> is to represent numeric scores that fall within a well defined
/// range. For example, it can be used to represent a score between 1 and 5, where 1 is considered a poor score, and 5
/// is considered an excellent score.
/// </para>
/// </remarks>
/// <param name="name">The name of the <see cref="NumericMetric"/>.</param>
/// <param name="value">The value of the <see cref="NumericMetric"/>.</param>
public sealed class NumericMetric(string name, double? value = null) : EvaluationMetric<double?>(name, value);
