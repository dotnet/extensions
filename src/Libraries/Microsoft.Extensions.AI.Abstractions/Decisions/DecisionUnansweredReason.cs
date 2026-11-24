// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the reason a provider did not answer a decision question.</summary>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
[JsonConverter(typeof(Converter))]
public readonly struct DecisionUnansweredReason : IEquatable<DecisionUnansweredReason>
{
    /// <summary>Initializes a new instance of the <see cref="DecisionUnansweredReason"/> struct with a string that describes the reason.</summary>
    /// <param name="value">The reason value.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> is empty or composed entirely of whitespace.</exception>
    [JsonConstructor]
    public DecisionUnansweredReason(string value)
    {
        Value = Throw.IfNullOrWhitespace(value);
    }

    /// <summary>Gets a <see cref="DecisionUnansweredReason"/> representing the model declining to answer the question.</summary>
    public static DecisionUnansweredReason Refusal { get; } = new("refusal");

    /// <summary>Gets the reason value.</summary>
    public string Value => field ?? Refusal.Value;

    /// <summary>
    /// Compares two instances.
    /// </summary>
    /// <param name="left">The left argument of the comparison.</param>
    /// <param name="right">The right argument of the comparison.</param>
    /// <returns><see langword="true" /> if the two instances are equal; <see langword="false" /> if they aren't equal.</returns>
    public static bool operator ==(DecisionUnansweredReason left, DecisionUnansweredReason right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two instances.
    /// </summary>
    /// <param name="left">The left argument of the comparison.</param>
    /// <param name="right">The right argument of the comparison.</param>
    /// <returns><see langword="true" /> if the two instances aren't equal; <see langword="false" /> if they are equal.</returns>
    public static bool operator !=(DecisionUnansweredReason left, DecisionUnansweredReason right)
    {
        return !(left == right);
    }

    /// <inheritdoc />
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is DecisionUnansweredReason other && Equals(other);

    /// <inheritdoc />
    public bool Equals(DecisionUnansweredReason other) => StringComparer.OrdinalIgnoreCase.Equals(Value, other.Value);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    /// <summary>Gets the <see cref="Value"/> of the reason.</summary>
    /// <returns>The <see cref="Value"/> of the reason.</returns>
    public override string ToString() => Value;

    /// <summary>Provides a <see cref="JsonConverter{DecisionUnansweredReason}"/> for serializing <see cref="DecisionUnansweredReason"/> instances.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class Converter : JsonConverter<DecisionUnansweredReason>
    {
        /// <inheritdoc/>
        public override DecisionUnansweredReason Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetString()!);

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, DecisionUnansweredReason value, JsonSerializerOptions options) =>
            Throw.IfNull(writer).WriteStringValue(value.Value);
    }
}
