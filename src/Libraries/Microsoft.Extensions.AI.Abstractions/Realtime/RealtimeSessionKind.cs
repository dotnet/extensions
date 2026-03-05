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
/// Represents the kind of a real-time session.
/// </summary>
/// <remarks>
/// Well-known session kinds are provided as static properties. Providers may define additional
/// session kinds by constructing new instances with custom values.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIRealTime, UrlFormat = DiagnosticIds.UrlFormat)]
[JsonConverter(typeof(Converter))]
[DebuggerDisplay("{Value,nq}")]
public readonly struct RealtimeSessionKind : IEquatable<RealtimeSessionKind>
{
    /// <summary>
    /// Gets a session kind representing a conversational session which processes audio, text, or other media in real-time.
    /// </summary>
    public static RealtimeSessionKind Conversation { get; } = new("conversation");

    /// <summary>
    /// Gets a session kind representing a transcription-only session.
    /// </summary>
    public static RealtimeSessionKind Transcription { get; } = new("transcription");

    /// <summary>Gets the value of the session kind.</summary>
    public string Value { get; }

    /// <summary>Initializes a new instance of the <see cref="RealtimeSessionKind"/> struct with the provided value.</summary>
    /// <param name="value">The value to associate with this <see cref="RealtimeSessionKind"/>.</param>
    [JsonConstructor]
    public RealtimeSessionKind(string value)
    {
        Value = Throw.IfNullOrWhitespace(value);
    }

    /// <summary>
    /// Returns a value indicating whether two <see cref="RealtimeSessionKind"/> instances are equivalent, as determined by a
    /// case-insensitive comparison of their values.
    /// </summary>
    /// <param name="left">The first instance to compare.</param>
    /// <param name="right">The second instance to compare.</param>
    /// <returns><see langword="true"/> if left and right have equivalent values; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(RealtimeSessionKind left, RealtimeSessionKind right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Returns a value indicating whether two <see cref="RealtimeSessionKind"/> instances are not equivalent, as determined by a
    /// case-insensitive comparison of their values.
    /// </summary>
    /// <param name="left">The first instance to compare.</param>
    /// <param name="right">The second instance to compare.</param>
    /// <returns><see langword="true"/> if left and right have different values; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(RealtimeSessionKind left, RealtimeSessionKind right)
    {
        return !(left == right);
    }

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is RealtimeSessionKind other && Equals(other);

    /// <inheritdoc/>
    public bool Equals(RealtimeSessionKind other)
        => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public override int GetHashCode()
        => Value is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    /// <inheritdoc/>
    public override string ToString() => Value ?? string.Empty;

    /// <summary>Provides a <see cref="JsonConverter{RealtimeSessionKind}"/> for serializing <see cref="RealtimeSessionKind"/> instances.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class Converter : JsonConverter<RealtimeSessionKind>
    {
        /// <inheritdoc />
        public override RealtimeSessionKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetString()!);

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, RealtimeSessionKind value, JsonSerializerOptions options) =>
            Throw.IfNull(writer).WriteStringValue(value.Value);
    }
}
