// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Describes the unit in which OCR geometry coordinates (<see cref="OcrPoint"/> and
/// <see cref="OcrBoundingBox"/>) are expressed.
/// </summary>
/// <remarks>
/// Coordinate conventions differ across OCR engines: some report pixels of the rendered page image,
/// some report a physical unit such as inches, and some normalize to the page. Pairing the geometry
/// with an <see cref="OcrCoordinateUnit"/> and the page dimensions (<see cref="OcrPage.Width"/> and
/// <see cref="OcrPage.Height"/>) lets a consumer interpret or normalize regions with engine-agnostic
/// code. This type is a small open set modeled on <see cref="ChatRole"/>: the well-known values cover
/// the common cases, and a provider may introduce its own value when needed.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIOcr, UrlFormat = DiagnosticIds.UrlFormat)]
[JsonConverter(typeof(Converter))]
[DebuggerDisplay("{Value,nq}")]
public readonly struct OcrCoordinateUnit : IEquatable<OcrCoordinateUnit>
{
    /// <summary>Gets the unit for coordinates expressed in pixels of the rendered page image.</summary>
    public static OcrCoordinateUnit Pixel { get; } = new("pixel");

    /// <summary>Gets the unit for coordinates expressed in inches.</summary>
    public static OcrCoordinateUnit Inch { get; } = new("inch");

    /// <summary>Gets the unit for coordinates normalized to the range [0, 1] relative to the page width and height.</summary>
    public static OcrCoordinateUnit Normalized { get; } = new("normalized");

    /// <summary>Gets the value associated with this <see cref="OcrCoordinateUnit"/>.</summary>
    public string Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OcrCoordinateUnit"/> struct with the provided value.
    /// </summary>
    /// <param name="value">The value to associate with this <see cref="OcrCoordinateUnit"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> is empty or composed entirely of whitespace.</exception>
    [JsonConstructor]
    public OcrCoordinateUnit(string value)
    {
        Value = Throw.IfNullOrWhitespace(value);
    }

    /// <summary>
    /// Returns a value indicating whether two <see cref="OcrCoordinateUnit"/> instances are equivalent, as
    /// determined by a case-insensitive comparison of their values.
    /// </summary>
    /// <param name="left">The first <see cref="OcrCoordinateUnit"/> instance to compare.</param>
    /// <param name="right">The second <see cref="OcrCoordinateUnit"/> instance to compare.</param>
    /// <returns><see langword="true"/> if left and right have equivalent values; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(OcrCoordinateUnit left, OcrCoordinateUnit right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Returns a value indicating whether two <see cref="OcrCoordinateUnit"/> instances are not equivalent, as
    /// determined by a case-insensitive comparison of their values.
    /// </summary>
    /// <param name="left">The first <see cref="OcrCoordinateUnit"/> instance to compare.</param>
    /// <param name="right">The second <see cref="OcrCoordinateUnit"/> instance to compare.</param>
    /// <returns><see langword="true"/> if left and right have different values; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(OcrCoordinateUnit left, OcrCoordinateUnit right)
    {
        return !(left == right);
    }

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is OcrCoordinateUnit otherUnit && Equals(otherUnit);

    /// <inheritdoc/>
    public bool Equals(OcrCoordinateUnit other)
        => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public override int GetHashCode()
        => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    /// <inheritdoc/>
    public override string ToString() => Value;

    /// <summary>Provides a <see cref="JsonConverter{OcrCoordinateUnit}"/> for serializing <see cref="OcrCoordinateUnit"/> instances.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class Converter : JsonConverter<OcrCoordinateUnit>
    {
        /// <inheritdoc/>
        public override OcrCoordinateUnit Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetString()!);

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, OcrCoordinateUnit value, JsonSerializerOptions options) =>
            Throw.IfNull(writer).WriteStringValue(value.Value);
    }
}
