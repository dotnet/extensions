// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI.Evaluation;

/// <summary>
/// An enumeration that identifies the set of possible values for <see cref="EvaluationDiagnostic.Severity"/>.
/// </summary>
public enum EvaluationDiagnosticSeverity
{
    /// <summary>
    /// A value that indicates that the <see cref="EvaluationDiagnostic"/> is informational.
    /// </summary>
    Informational,

    /// <summary>
    /// A value that indicates that the <see cref="EvaluationDiagnostic"/> represents a warning.
    /// </summary>
    Warning,

    /// <summary>
    /// A value that indicates that the <see cref="EvaluationDiagnostic"/> represents an error.
    /// </summary>
    Error
}
