// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Compliance.Classification;

/// <summary>
/// Represents a single classification which is part of a data taxonomy.
/// </summary>
[TypeConverter(typeof(DataClassificationTypeConverter))]
public readonly struct DataClassification : IEquatable<DataClassification>
{
    /// <summary>
    /// Gets the value to represent data with no defined classification.
    /// </summary>
    public static DataClassification None => new(nameof(None));

    /// <summary>
    /// Gets the value to represent data with an unknown classification.
    /// </summary>
    public static DataClassification Unknown => new(nameof(Unknown));

    /// <summary>
    /// Gets the name of the taxonomy that recognizes this classification.
    /// </summary>
    public string TaxonomyName { get; }

    /// <summary>
    /// Gets the value representing the classification within the taxonomy.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataClassification"/> struct.
    /// </summary>
    /// <param name="taxonomyName">The name of the data taxonomy this classification belongs to.</param>
    /// <param name="value">The taxonomy-specific value representing the classification.</param>
    public DataClassification(string taxonomyName, string value)
    {
        TaxonomyName = Throw.IfNullOrWhitespace(taxonomyName);
        Value = Throw.IfNullOrWhitespace(value);
    }

    private DataClassification(string value)
    {
        TaxonomyName = string.Empty;
        Value = value;
    }

    /// <summary>
    /// Checks if an object is equal to this instance of <see cref="DataClassification"/>.
    /// </summary>
    /// <param name="obj">Object to check for equality.</param>
    /// <returns><see langword="true" /> if object instances are equal <see langword="false" /> otherwise.</returns>
    public override bool Equals(object? obj) => (obj is DataClassification dc) && Equals(dc);

    /// <summary>
    /// Checks if an object is equal to this instance of <see cref="DataClassification"/>.
    /// </summary>
    /// <param name="other">Instance of <see cref="DataClassification"/> to check for equality.</param>
    /// <returns><see langword="true" /> if object instances are equal <see langword="false" /> otherwise.</returns>
    public bool Equals(DataClassification other) => other.TaxonomyName == TaxonomyName && other.Value == Value;

    /// <summary>
    /// Get the hash code for the current instance.
    /// </summary>
    /// <returns>Hash code.</returns>
    public override int GetHashCode() => HashCode.Combine(TaxonomyName, Value);

    /// <summary>
    /// Check if two instances are equal.
    /// </summary>
    /// <param name="left">Left argument of the comparison.</param>
    /// <param name="right">Right argument of the comparison.</param>
    /// <returns><see langword="true" /> if object instances are equal, or <see langword="false" /> otherwise.</returns>
    public static bool operator ==(DataClassification left, DataClassification right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Check if two instances are not equal.
    /// </summary>
    /// <param name="left">Left argument of the comparison.</param>
    /// <param name="right">Right argument of the comparison.</param>
    /// <returns><see langword="false" /> if object instances are equal, or <see langword="true" /> otherwise.</returns>
    public static bool operator !=(DataClassification left, DataClassification right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Gets a string representation of this object.
    /// </summary>
    /// <returns>A string representing the object.</returns>
    public override string ToString() => string.IsNullOrWhiteSpace(TaxonomyName) ? Value : $"{TaxonomyName}:{Value}";
}
