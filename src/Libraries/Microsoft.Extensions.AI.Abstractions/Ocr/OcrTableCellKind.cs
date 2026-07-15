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

/// <summary>Describes the role of an <see cref="OcrTableCell"/>, such as a column header or a content cell.</summary>
/// <remarks>
/// This is a small open set modeled on <see cref="ChatRole"/>: the well-known values cover the common
/// table cell roles, and a provider may introduce its own value when an engine reports a role that is
/// not represented here.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIOcr, UrlFormat = DiagnosticIds.UrlFormat)]
[JsonConverter(typeof(Converter))]
[DebuggerDisplay("{Value,nq}")]
public readonly struct OcrTableCellKind : IEquatable<OcrTableCellKind>
{
    /// <summary>Gets the kind representing a column header cell.</summary>
    public static OcrTableCellKind ColumnHeader { get; } = new("columnHeader");

    /// <summary>Gets the kind representing a regular content cell.</summary>
    public static OcrTableCellKind Content { get; } = new("content");

    /// <summary>Gets the value associated with this <see cref="OcrTableCellKind"/>.</summary>
    public string Value { get; }

    /// <summary>Initializes a new instance of the <see cref="OcrTableCellKind"/> struct with the provided value.</summary>
    /// <param name="value">The value to associate with this <see cref="OcrTableCellKind"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> is empty or composed entirely of whitespace.</exception>
    [JsonConstructor]
    public OcrTableCellKind(string value)
    {
        Value = Throw.IfNullOrWhitespace(value);
    }

    /// <summary>Returns a value indicating whether two <see cref="OcrTableCellKind"/> instances are equivalent, using a case-insensitive comparison.</summary>
    /// <param name="left">The first <see cref="OcrTableCellKind"/> instance to compare.</param>
    /// <param name="right">The second <see cref="OcrTableCellKind"/> instance to compare.</param>
    /// <returns><see langword="true"/> if left and right have equivalent values; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(OcrTableCellKind left, OcrTableCellKind right)
    {
        return left.Equals(right);
    }

    /// <summary>Returns a value indicating whether two <see cref="OcrTableCellKind"/> instances are not equivalent, using a case-insensitive comparison.</summary>
    /// <param name="left">The first <see cref="OcrTableCellKind"/> instance to compare.</param>
    /// <param name="right">The second <see cref="OcrTableCellKind"/> instance to compare.</param>
    /// <returns><see langword="true"/> if left and right have different values; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(OcrTableCellKind left, OcrTableCellKind right)
    {
        return !(left == right);
    }

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is OcrTableCellKind otherKind && Equals(otherKind);

    /// <inheritdoc/>
    public bool Equals(OcrTableCellKind other)
        => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public override int GetHashCode()
        => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    /// <inheritdoc/>
    public override string ToString() => Value;

    /// <summary>Provides a <see cref="JsonConverter{OcrTableCellKind}"/> for serializing <see cref="OcrTableCellKind"/> instances.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class Converter : JsonConverter<OcrTableCellKind>
    {
        /// <inheritdoc/>
        public override OcrTableCellKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetString()!);

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, OcrTableCellKind value, JsonSerializerOptions options) =>
            Throw.IfNull(writer).WriteStringValue(value.Value);
    }
}
