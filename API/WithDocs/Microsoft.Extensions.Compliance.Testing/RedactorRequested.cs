// Assembly 'Microsoft.Extensions.Compliance.Testing'

using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.Extensions.Compliance.Testing;

/// <summary>
/// State representing a single request for a redactor.
/// </summary>
public readonly struct RedactorRequested : IEquatable<RedactorRequested>
{
    /// <summary>
    /// Gets the the data classification for which the redactor was returned.
    /// </summary>
    public DataClassification DataClassification { get; }

    /// <summary>
    /// Gets the order in which the redactor was requested.
    /// </summary>
    public int SequenceNumber { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Compliance.Testing.RedactorRequested" /> struct.
    /// </summary>
    /// <param name="classification">Data class for which redactor was used.</param>
    /// <param name="sequenceNumber">Order in which the request was used.</param>
    public RedactorRequested(DataClassification classification, int sequenceNumber);

    /// <summary>
    /// Checks if object is equal to this instance of <see cref="T:System.Object" />.
    /// </summary>
    /// <param name="obj">Object to check for equality.</param>
    /// <returns><see langword="true" /> if object instances are equal <see langword="false" /> otherwise.</returns>
    public override bool Equals(object? obj);

    /// <summary>
    /// Checks if object is equal to this instance of <see cref="T:Microsoft.Extensions.Compliance.Testing.RedactorRequested" />.
    /// </summary>
    /// <param name="other">Instance to check for equality.</param>
    /// <returns><see langword="true" /> if object instances are equal <see langword="false" /> otherwise.</returns>
    public bool Equals(RedactorRequested other);

    /// <summary>
    /// Get hashcode of given <see cref="T:Microsoft.Extensions.Compliance.Testing.RedactorRequested" />.
    /// </summary>
    /// <returns>Hash code.</returns>
    public override int GetHashCode();

    /// <summary>
    /// Compares two instances.
    /// </summary>
    /// <param name="left">Left argument of the comparison.</param>
    /// <param name="right">Right argument of the comparison.</param>
    /// <returns><see langword="true" /> when equal, <see langword="false" /> otherwise.</returns>
    public static bool operator ==(RedactorRequested left, RedactorRequested right);

    /// <summary>
    /// Compares two instances.
    /// </summary>
    /// <param name="left">Left argument of the comparison.</param>
    /// <param name="right">Right argument of the comparison.</param>
    /// <returns><see langword="true" /> when not equal, <see langword="false" /> otherwise.</returns>
    public static bool operator !=(RedactorRequested left, RedactorRequested right);
}
