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

namespace Microsoft.Extensions.DocumentExtraction;

/// <summary>Describes the role of an <see cref="DocumentTableCell"/>, such as a column header or a content cell.</summary>
/// <remarks>
/// This is a small open set modeled on <see cref="Microsoft.Extensions.AI.ChatRole"/>: the well-known values cover the common
/// table cell roles, and a provider may introduce its own value when an engine reports a role that is
/// not represented here.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.DocumentExtraction, UrlFormat = DiagnosticIds.UrlFormat)]
[JsonConverter(typeof(Converter))]
[DebuggerDisplay("{Value,nq}")]
public readonly struct DocumentTableCellKind : IEquatable<DocumentTableCellKind>
{
    /// <summary>Gets the kind representing a column header cell.</summary>
    public static DocumentTableCellKind ColumnHeader { get; } = new("columnHeader");

    /// <summary>Gets the kind representing a regular content cell.</summary>
    public static DocumentTableCellKind Content { get; } = new("content");

    /// <summary>Gets the kind representing a row header cell (a header that labels the row it sits in).</summary>
    public static DocumentTableCellKind RowHeader { get; } = new("rowHeader");

    /// <summary>Gets the kind representing a cell that introduces a labeled section spanning subsequent rows.</summary>
    public static DocumentTableCellKind RowSection { get; } = new("rowSection");

    /// <summary>Gets the value associated with this <see cref="DocumentTableCellKind"/>.</summary>
    public string Value { get; }

    /// <summary>Initializes a new instance of the <see cref="DocumentTableCellKind"/> struct with the provided value.</summary>
    /// <param name="value">The value to associate with this <see cref="DocumentTableCellKind"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> is empty or composed entirely of whitespace.</exception>
    [JsonConstructor]
    public DocumentTableCellKind(string value)
    {
        Value = Throw.IfNullOrWhitespace(value);
    }

    /// <summary>Returns a value indicating whether two <see cref="DocumentTableCellKind"/> instances are equivalent, using a case-insensitive comparison.</summary>
    /// <param name="left">The first <see cref="DocumentTableCellKind"/> instance to compare.</param>
    /// <param name="right">The second <see cref="DocumentTableCellKind"/> instance to compare.</param>
    /// <returns><see langword="true"/> if left and right have equivalent values; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(DocumentTableCellKind left, DocumentTableCellKind right)
    {
        return left.Equals(right);
    }

    /// <summary>Returns a value indicating whether two <see cref="DocumentTableCellKind"/> instances are not equivalent, using a case-insensitive comparison.</summary>
    /// <param name="left">The first <see cref="DocumentTableCellKind"/> instance to compare.</param>
    /// <param name="right">The second <see cref="DocumentTableCellKind"/> instance to compare.</param>
    /// <returns><see langword="true"/> if left and right have different values; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(DocumentTableCellKind left, DocumentTableCellKind right)
    {
        return !(left == right);
    }

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is DocumentTableCellKind otherKind && Equals(otherKind);

    /// <inheritdoc/>
    public bool Equals(DocumentTableCellKind other)
        => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public override int GetHashCode()
        => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    /// <inheritdoc/>
    public override string ToString() => Value;

    /// <summary>Provides a <see cref="JsonConverter{DocumentTableCellKind}"/> for serializing <see cref="DocumentTableCellKind"/> instances.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class Converter : JsonConverter<DocumentTableCellKind>
    {
        /// <inheritdoc/>
        public override DocumentTableCellKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetString()!);

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, DocumentTableCellKind value, JsonSerializerOptions options) =>
            Throw.IfNull(writer).WriteStringValue(value.Value);
    }
}
