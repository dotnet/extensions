// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI.Evaluation;

/// <summary>
/// An <see cref="EvaluationMetric"/> containing a <see cref="string"/> value.
/// </summary>
/// <remarks>
/// A common use case for <see cref="StringMetric"/> is to represent a single value in an enumeration (or to represent
/// one value out of a set of possible values).
/// </remarks>
/// <param name="name">The name of the <see cref="StringMetric"/>.</param>
/// <param name="value">The value of the <see cref="StringMetric"/>.</param>
/// <param name="reason">
/// An optional string that can be used to provide some commentary around the result represented by this
/// <see cref="StringMetric"/>.
/// </param>
public sealed class StringMetric(string name, string? value = null, string? reason = null)
    : EvaluationMetric<string>(name, value, reason);
