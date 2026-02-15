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
/// Represents the eagerness level for semantic voice activity detection.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AIRealTime, UrlFormat = DiagnosticIds.UrlFormat)]
[JsonConverter(typeof(Converter))]
[DebuggerDisplay("{Value,nq}")]
public readonly struct SemanticEagerness : IEquatable<SemanticEagerness>
{
    /// <summary>Gets a value representing low eagerness.</summary>
    public static SemanticEagerness Low { get; } = new("low");

    /// <summary>Gets a value representing medium eagerness.</summary>
    public static SemanticEagerness Medium { get; } = new("medium");

    /// <summary>Gets a value representing high eagerness.</summary>
    public static SemanticEagerness High { get; } = new("high");

    /// <summary>Gets a value representing automatic eagerness detection.</summary>
    public static SemanticEagerness Auto { get; } = new("auto");

    /// <summary>
    /// Gets the value associated with this <see cref="SemanticEagerness"/>.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticEagerness"/> struct with the provided value.
    /// </summary>
    /// <param name="value">The value to associate with this <see cref="SemanticEagerness"/>.</param>
    [JsonConstructor]
    public SemanticEagerness(string value)
    {
        Value = Throw.IfNullOrWhitespace(value);
    }

    /// <summary>
    /// Returns a value indicating whether two <see cref="SemanticEagerness"/> instances are equivalent, as determined by a
    /// case-insensitive comparison of their values.
    /// </summary>
    public static bool operator ==(SemanticEagerness left, SemanticEagerness right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Returns a value indicating whether two <see cref="SemanticEagerness"/> instances are not equivalent, as determined by a
    /// case-insensitive comparison of their values.
    /// </summary>
    public static bool operator !=(SemanticEagerness left, SemanticEagerness right)
    {
        return !(left == right);
    }

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is SemanticEagerness other && Equals(other);

    /// <inheritdoc/>
    public bool Equals(SemanticEagerness other)
        => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public override int GetHashCode()
        => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    /// <inheritdoc/>
    public override string ToString() => Value;

    /// <summary>Provides a <see cref="JsonConverter{SemanticEagerness}"/> for serializing <see cref="SemanticEagerness"/> instances.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class Converter : JsonConverter<SemanticEagerness>
    {
        /// <inheritdoc />
        public override SemanticEagerness Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetString()!);

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, SemanticEagerness value, JsonSerializerOptions options) =>
            Throw.IfNull(writer).WriteStringValue(value.Value);
    }
}
