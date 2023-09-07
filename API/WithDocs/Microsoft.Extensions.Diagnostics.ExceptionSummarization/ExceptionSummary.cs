// Assembly 'Microsoft.Extensions.Diagnostics.ExceptionSummarization'

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Diagnostics.ExceptionSummarization;

/// <summary>
/// Holds a summary of an exception for use in telemetry.
/// </summary>
/// <remarks>
/// Metric tags typically support a limited number of distinct values, and as such they are not suitable
/// to represent values which are highly variable, such as the result of <see cref="M:System.Exception.ToString" />.
/// An exception summary represents a low-cardinality version of an exception's information, suitable for such
/// cases. The summary never includes sensitive information.
/// </remarks>
public readonly struct ExceptionSummary : IEquatable<ExceptionSummary>
{
    /// <summary>
    /// Gets the type description of the exception.
    /// </summary>
    /// <remarks>
    /// This is not guaranteed to be a type name. In particular, for inner exceptions, this will include the
    /// type name of the outer exception along with the type name of the inner exception.
    /// </remarks>
    public string ExceptionType { get; }

    /// <summary>
    /// Gets the summary description of the exception.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the additional details of the exception.
    /// </summary>
    /// <remarks>
    /// This string can have a relatively high cardinality and is therefore not suitable as a metric dimension. It
    /// is primarily intended for use in low-level diagnostics.
    /// </remarks>
    public string AdditionalDetails { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Diagnostics.ExceptionSummarization.ExceptionSummary" /> struct.
    /// </summary>
    /// <param name="exceptionType">The type of the exception.</param>
    /// <param name="description">A summary description string for telemetry.</param>
    /// <param name="additionalDetails">An additional details string, primarily for diagnostics and not telemetry.</param>
    public ExceptionSummary(string exceptionType, string description, string additionalDetails);

    /// <summary>
    /// Gets a hash code for this object.
    /// </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode();

    /// <summary>
    /// Gets a string representation of this object.
    /// </summary>
    /// <returns>A string representing this object.</returns>
    public override string ToString();

    /// <summary>
    /// Determines whether this summary and a specified other summary are identical.
    /// </summary>
    /// <param name="obj">The other summary.</param>
    /// <returns><see langword="true" /> if the two summaries are identical; otherwise, <see langword="false" />.</returns>
    public override bool Equals(object? obj);

    /// <summary>
    /// Determines whether this summary and a specified other summary are identical.
    /// </summary>
    /// <param name="other">The other summary.</param>
    /// <returns><see langword="true" /> if the two summaries are identical; otherwise, <see langword="false" />.</returns>
    public bool Equals(ExceptionSummary other);

    /// <summary>
    /// Equality operator.
    /// </summary>
    /// <param name="left">First value.</param>
    /// <param name="right">Second value.</param>
    /// <returns><see langword="true" /> if the operands are equal, <see langword="false" /> otherwise.</returns>
    public static bool operator ==(ExceptionSummary left, ExceptionSummary right);

    /// <summary>
    /// Inequality operator.
    /// </summary>
    /// <param name="left">First value.</param>
    /// <param name="right">Second value.</param>
    /// <returns><see langword="true" /> if the operands are equal, <see langword="false" /> otherwise.</returns>
    public static bool operator !=(ExceptionSummary left, ExceptionSummary right);
}
