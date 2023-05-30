// Assembly 'Microsoft.Extensions.Compliance.Abstractions'

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Compliance.Classification;

/// <summary>
/// Represents a set of data classes as a part of a data taxonomy.
/// </summary>
public readonly struct DataClassification : IEquatable<DataClassification>
{
    /// <summary>
    /// Represents unclassified data.
    /// </summary>
    public const ulong NoneTaxonomyValue = 0uL;

    /// <summary>
    /// Represents the unknown classification.
    /// </summary>
    public const ulong UnknownTaxonomyValue = 9223372036854775808uL;

    /// <summary>
    /// Gets the value to represent data with no defined classification.
    /// </summary>
    public static DataClassification None { get; }

    /// <summary>
    /// Gets the value to represent data with an unknown classification.
    /// </summary>
    public static DataClassification Unknown { get; }

    /// <summary>
    /// Gets the name of the taxonomy that recognizes this classification.
    /// </summary>
    public string TaxonomyName { get; }

    /// <summary>
    /// Gets the bit mask representing the data classes.
    /// </summary>
    public ulong Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Compliance.Classification.DataClassification" /> struct.
    /// </summary>
    /// <param name="taxonomyName">Name of the taxonomy this classification belongs to.</param>
    /// <param name="value">The taxonomy-specific bit vector representing the data classes.</param>
    /// <exception cref="T:System.ArgumentException">If bit 63, corresponding to <see cref="F:Microsoft.Extensions.Compliance.Classification.DataClassification.UnknownTaxonomyValue" /> is set in the <paramref name="value" /> value.</exception>
    public DataClassification(string taxonomyName, ulong value);

    /// <summary>
    /// Checks if object is equal to this instance of <see cref="T:Microsoft.Extensions.Compliance.Classification.DataClassification" />.
    /// </summary>
    /// <param name="obj">Object to check for equality.</param>
    /// <returns><see langword="true" /> if object instances are equal <see langword="false" /> otherwise.</returns>
    public override bool Equals(object? obj);

    /// <summary>
    /// Checks if object is equal to this instance of <see cref="T:Microsoft.Extensions.Compliance.Classification.DataClassification" />.
    /// </summary>
    /// <param name="other">Instance of <see cref="T:Microsoft.Extensions.Compliance.Classification.DataClassification" /> to check for equality.</param>
    /// <returns><see langword="true" /> if object instances are equal <see langword="false" /> otherwise.</returns>
    public bool Equals(DataClassification other);

    /// <summary>
    /// Get the hash code the current instance.
    /// </summary>
    /// <returns>Hash code.</returns>
    public override int GetHashCode();

    /// <summary>
    /// Check if two instances are equal.
    /// </summary>
    /// <param name="left">Left argument of the comparison.</param>
    /// <param name="right">Right argument of the comparison.</param>
    /// <returns><see langword="true" /> if object instances are equal, or <see langword="false" /> otherwise.</returns>
    public static bool operator ==(DataClassification left, DataClassification right);

    /// <summary>
    /// Check if two instances are not equal.
    /// </summary>
    /// <param name="left">Left argument of the comparison.</param>
    /// <param name="right">Right argument of the comparison.</param>
    /// <returns><see langword="false" /> if object instances are equal, or <see langword="true" /> otherwise.</returns>
    public static bool operator !=(DataClassification left, DataClassification right);

    /// <summary>
    /// Combines together two data classifications.
    /// </summary>
    /// <param name="left">The first classification to combine.</param>
    /// <param name="right">The second classification to combine.</param>
    /// <returns>A new classification object representing the combination of the two input classifications.</returns>
    /// <exception cref="T:System.ArgumentException">if the two classifications aren't part of the same taxonomy.</exception>
    public static DataClassification Combine(DataClassification left, DataClassification right);

    /// <summary>
    /// Combines together two data classifications.
    /// </summary>
    /// <param name="left">The first classification to combine.</param>
    /// <param name="right">The second classification to combine.</param>
    /// <returns>A new classification object representing the combination of the two input classifications.</returns>
    /// <exception cref="T:System.ArgumentException">if the two classifications aren't part of the same taxonomy.</exception>
    public static DataClassification operator |(DataClassification left, DataClassification right);
}
