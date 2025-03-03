// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI.Evaluation;

/// <summary>
/// An <see cref="EvaluationMetric"/> containing a <see cref="bool"/> value that can be used to represent an outcome
/// that can have one of two possible values (such as yes v/s no, or pass v/s fail).
/// </summary>
/// <param name="name">The name of the <see cref="BooleanMetric"/>.</param>
/// <param name="value">The value of the <see cref="BooleanMetric"/>.</param>
public sealed class BooleanMetric(string name, bool? value = null) : EvaluationMetric<bool?>(name, value);
