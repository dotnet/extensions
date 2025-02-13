// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

namespace Microsoft.Extensions.AI.Evaluation;

/// <summary>
/// Represents a diagnostic (such as a warning, error or informational message) that applies to the result represented
/// in an <see cref="EvaluationMetric"/>.
/// </summary>
/// <param name="severity">
/// An <see cref="EvaluationDiagnosticSeverity"/> that indicates the severity of the
/// <see cref="EvaluationDiagnostic"/>.
/// </param>
/// <param name="message">
/// An error, warning or informational message describing the <see cref="EvaluationDiagnostic"/>.
/// </param>
public sealed class EvaluationDiagnostic(EvaluationDiagnosticSeverity severity, string message)
{
    /// <summary>
    /// Gets or sets an <see cref="EvaluationDiagnosticSeverity"/> that indicates the severity of the
    /// <see cref="EvaluationDiagnostic"/>.
    /// </summary>
    public EvaluationDiagnosticSeverity Severity { get; set; } = severity;

    /// <summary>
    /// Gets or sets an error, warning or informational message describing the <see cref="EvaluationDiagnostic"/>.
    /// </summary>
    public string Message { get; set; } = message;

    /// <summary>
    /// Returns an <see cref="EvaluationDiagnostic"/> with the supplied <paramref name="message"/> and with
    /// <see cref="Severity"/> set to <see cref="EvaluationDiagnosticSeverity.Informational"/>.
    /// </summary>
    /// <param name="message">An informational message describing the <see cref="EvaluationDiagnostic"/>.</param>
    /// <returns>
    /// An <see cref="EvaluationDiagnostic"/> with <see cref="Severity"/> set to
    /// <see cref="EvaluationDiagnosticSeverity.Informational"/>.
    /// </returns>
    public static EvaluationDiagnostic Informational(string message)
        => new EvaluationDiagnostic(EvaluationDiagnosticSeverity.Informational, message);

    /// <summary>
    /// Returns an <see cref="EvaluationDiagnostic"/> with the supplied <paramref name="message"/> and with
    /// <see cref="Severity"/> set to <see cref="EvaluationDiagnosticSeverity.Warning"/>.
    /// </summary>
    /// <param name="message">A warning message describing the <see cref="EvaluationDiagnostic"/>.</param>
    /// <returns>
    /// An <see cref="EvaluationDiagnostic"/> with <see cref="Severity"/> set to
    /// <see cref="EvaluationDiagnosticSeverity.Warning"/>.
    /// </returns>
    public static EvaluationDiagnostic Warning(string message)
        => new EvaluationDiagnostic(EvaluationDiagnosticSeverity.Warning, message);

    /// <summary>
    /// Returns an <see cref="EvaluationDiagnostic"/> with the supplied <paramref name="message"/> and with
    /// <see cref="Severity"/> set to <see cref="EvaluationDiagnosticSeverity.Error"/>.
    /// </summary>
    /// <param name="message">An error message describing the <see cref="EvaluationDiagnostic"/>.</param>
    /// <returns>
    /// An <see cref="EvaluationDiagnostic"/> with <see cref="Severity"/> set to
    /// <see cref="EvaluationDiagnosticSeverity.Error"/>.
    /// </returns>
    public static EvaluationDiagnostic Error(string message)
        => new EvaluationDiagnostic(EvaluationDiagnosticSeverity.Error, message);
}
