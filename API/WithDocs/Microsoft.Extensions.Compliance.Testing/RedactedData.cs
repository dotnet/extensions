// Assembly 'Microsoft.Extensions.Compliance.Testing'

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Compliance.Testing;

/// <summary>
/// State representing a single redaction "event".
/// </summary>
public readonly struct RedactedData : IEquatable<RedactedData>
{
    /// <summary>
    /// Gets the original data that got redacted.
    /// </summary>
    public string Original { get; }

    /// <summary>
    /// Gets the redacted data.
    /// </summary>
    public string Redacted { get; }

    /// <summary>
    /// Gets the order in which data was redacted.
    /// </summary>
    public int SequenceNumber { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Compliance.Testing.RedactedData" /> struct.
    /// </summary>
    /// <param name="original">Data that was redacted.</param>
    /// <param name="redacted">Redacted data.</param>
    /// <param name="sequenceNumber">Order in which data were redacted.</param>
    public RedactedData(string original, string redacted, int sequenceNumber);

    /// <summary>
    /// Checks if object is equal to this instance of <see cref="T:System.Object" />.
    /// </summary>
    /// <param name="obj">Object to check for equality.</param>
    /// <returns><see langword="true" /> if object instances are equal <see langword="false" /> otherwise.</returns>
    public override bool Equals(object? obj);

    /// <summary>
    /// Checks if object is equal to this instance of <see cref="T:Microsoft.Extensions.Compliance.Testing.RedactedData" />.
    /// </summary>
    /// <param name="other">Instance to check for equality.</param>
    /// <returns><see langword="true" /> if object instances are equal <see langword="false" /> otherwise.</returns>
    public bool Equals(RedactedData other);

    /// <summary>
    /// Get hashcode of given <see cref="T:Microsoft.Extensions.Compliance.Testing.RedactedData" />.
    /// </summary>
    /// <returns>Hash code.</returns>
    public override int GetHashCode();

    /// <summary>
    /// Compares two instances.
    /// </summary>
    /// <param name="left">Left argument of the comparison.</param>
    /// <param name="right">Right argument of the comparison.</param>
    /// <returns><see langword="true" /> when equal, <see langword="false" /> otherwise.</returns>
    public static bool operator ==(RedactedData left, RedactedData right);

    /// <summary>
    /// Compares two instances.
    /// </summary>
    /// <param name="left">Left argument of the comparison.</param>
    /// <param name="right">Right argument of the comparison.</param>
    /// <returns><see langword="true" /> when not equal, <see langword="false" /> otherwise.</returns>
    public static bool operator !=(RedactedData left, RedactedData right);
}
