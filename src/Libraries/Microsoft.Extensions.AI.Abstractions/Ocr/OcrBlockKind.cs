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

/// <summary>Describes the kind of an <see cref="OcrBlock"/>, such as a paragraph, title, or figure.</summary>
/// <remarks>
/// This is a small open set modeled on <see cref="ChatRole"/>: the well-known values cover the common
/// layout categories, and a provider may introduce its own value when an engine reports a kind that is
/// not represented here.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIOcr, UrlFormat = DiagnosticIds.UrlFormat)]
[JsonConverter(typeof(Converter))]
[DebuggerDisplay("{Value,nq}")]
public readonly struct OcrBlockKind : IEquatable<OcrBlockKind>
{
    /// <summary>Gets the kind representing a paragraph of body text.</summary>
    public static OcrBlockKind Paragraph { get; } = new("paragraph");

    /// <summary>Gets the kind representing a title or heading.</summary>
    public static OcrBlockKind Title { get; } = new("title");

    /// <summary>Gets the kind representing a figure or image region.</summary>
    public static OcrBlockKind Figure { get; } = new("figure");

    /// <summary>Gets the value associated with this <see cref="OcrBlockKind"/>.</summary>
    public string Value { get; }

    /// <summary>Initializes a new instance of the <see cref="OcrBlockKind"/> struct with the provided value.</summary>
    /// <param name="value">The value to associate with this <see cref="OcrBlockKind"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> is empty or composed entirely of whitespace.</exception>
    [JsonConstructor]
    public OcrBlockKind(string value)
    {
        Value = Throw.IfNullOrWhitespace(value);
    }

    /// <summary>Returns a value indicating whether two <see cref="OcrBlockKind"/> instances are equivalent, using a case-insensitive comparison.</summary>
    /// <param name="left">The first <see cref="OcrBlockKind"/> instance to compare.</param>
    /// <param name="right">The second <see cref="OcrBlockKind"/> instance to compare.</param>
    /// <returns><see langword="true"/> if left and right have equivalent values; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(OcrBlockKind left, OcrBlockKind right)
    {
        return left.Equals(right);
    }

    /// <summary>Returns a value indicating whether two <see cref="OcrBlockKind"/> instances are not equivalent, using a case-insensitive comparison.</summary>
    /// <param name="left">The first <see cref="OcrBlockKind"/> instance to compare.</param>
    /// <param name="right">The second <see cref="OcrBlockKind"/> instance to compare.</param>
    /// <returns><see langword="true"/> if left and right have different values; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(OcrBlockKind left, OcrBlockKind right)
    {
        return !(left == right);
    }

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is OcrBlockKind otherKind && Equals(otherKind);

    /// <inheritdoc/>
    public bool Equals(OcrBlockKind other)
        => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public override int GetHashCode()
        => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    /// <inheritdoc/>
    public override string ToString() => Value;

    /// <summary>Provides a <see cref="JsonConverter{OcrBlockKind}"/> for serializing <see cref="OcrBlockKind"/> instances.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class Converter : JsonConverter<OcrBlockKind>
    {
        /// <inheritdoc/>
        public override OcrBlockKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetString()!);

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, OcrBlockKind value, JsonSerializerOptions options) =>
            Throw.IfNull(writer).WriteStringValue(value.Value);
    }
}
