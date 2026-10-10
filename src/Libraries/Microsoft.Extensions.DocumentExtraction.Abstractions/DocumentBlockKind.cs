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

/// <summary>Describes the kind of an <see cref="DocumentBlock"/>, such as a paragraph, title, or figure.</summary>
/// <remarks>
/// This is a small open set modeled on <see cref="Microsoft.Extensions.AI.ChatRole"/>: the well-known values cover the common
/// layout categories, and a provider may introduce its own value when an engine reports a kind that is
/// not represented here.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.DocumentExtraction, UrlFormat = DiagnosticIds.UrlFormat)]
[JsonConverter(typeof(Converter))]
[DebuggerDisplay("{Value,nq}")]
public readonly struct DocumentBlockKind : IEquatable<DocumentBlockKind>
{
    /// <summary>Gets the kind representing a paragraph of body text.</summary>
    public static DocumentBlockKind Paragraph { get; } = new("paragraph");

    /// <summary>Gets the kind representing a title or heading.</summary>
    public static DocumentBlockKind Title { get; } = new("title");

    /// <summary>Gets the kind representing a figure or image region.</summary>
    public static DocumentBlockKind Figure { get; } = new("figure");

    /// <summary>Gets the value associated with this <see cref="DocumentBlockKind"/>.</summary>
    public string Value { get; }

    /// <summary>Initializes a new instance of the <see cref="DocumentBlockKind"/> struct with the provided value.</summary>
    /// <param name="value">The value to associate with this <see cref="DocumentBlockKind"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> is empty or composed entirely of whitespace.</exception>
    [JsonConstructor]
    public DocumentBlockKind(string value)
    {
        Value = Throw.IfNullOrWhitespace(value);
    }

    /// <summary>Returns a value indicating whether two <see cref="DocumentBlockKind"/> instances are equivalent, using a case-insensitive comparison.</summary>
    /// <param name="left">The first <see cref="DocumentBlockKind"/> instance to compare.</param>
    /// <param name="right">The second <see cref="DocumentBlockKind"/> instance to compare.</param>
    /// <returns><see langword="true"/> if left and right have equivalent values; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(DocumentBlockKind left, DocumentBlockKind right)
    {
        return left.Equals(right);
    }

    /// <summary>Returns a value indicating whether two <see cref="DocumentBlockKind"/> instances are not equivalent, using a case-insensitive comparison.</summary>
    /// <param name="left">The first <see cref="DocumentBlockKind"/> instance to compare.</param>
    /// <param name="right">The second <see cref="DocumentBlockKind"/> instance to compare.</param>
    /// <returns><see langword="true"/> if left and right have different values; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(DocumentBlockKind left, DocumentBlockKind right)
    {
        return !(left == right);
    }

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is DocumentBlockKind otherKind && Equals(otherKind);

    /// <inheritdoc/>
    public bool Equals(DocumentBlockKind other)
        => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public override int GetHashCode()
        => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    /// <inheritdoc/>
    public override string ToString() => Value;

    /// <summary>Provides a <see cref="JsonConverter{DocumentBlockKind}"/> for serializing <see cref="DocumentBlockKind"/> instances.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class Converter : JsonConverter<DocumentBlockKind>
    {
        /// <inheritdoc/>
        public override DocumentBlockKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetString()!);

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, DocumentBlockKind value, JsonSerializerOptions options) =>
            Throw.IfNull(writer).WriteStringValue(value.Value);
    }
}
