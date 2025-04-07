// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Describes the intended purpose of a specific update during streaming of speech to text updates.
/// </summary>
[Experimental("MEAI001")]
[JsonConverter(typeof(Converter))]
public readonly struct SpeechToTextResponseUpdateKind : IEquatable<SpeechToTextResponseUpdateKind>
{
    /// <summary>Gets when the generated text session is opened.</summary>
    public static SpeechToTextResponseUpdateKind SessionOpen { get; } = new("sessionopen");

    /// <summary>Gets when a non-blocking error occurs during speech to text updates.</summary>
    public static SpeechToTextResponseUpdateKind Error { get; } = new("error");

    /// <summary>Gets when the text update is in progress, without waiting for silence.</summary>
    public static SpeechToTextResponseUpdateKind TextUpdating { get; } = new("textupdating");

    /// <summary>Gets when the text was generated after small period of silence.</summary>
    public static SpeechToTextResponseUpdateKind TextUpdated { get; } = new("textupdated");

    /// <summary>Gets when the generated text session is closed.</summary>
    public static SpeechToTextResponseUpdateKind SessionClose { get; } = new("sessionclose");

    /// <summary>
    /// Gets the value associated with this <see cref="SpeechToTextResponseUpdateKind"/>.
    /// </summary>
    /// <remarks>
    /// The value will be serialized into the "kind" message field of the speech to text update format.
    /// </remarks>
    public string Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpeechToTextResponseUpdateKind"/> struct with the provided value.
    /// </summary>
    /// <param name="value">The value to associate with this <see cref="SpeechToTextResponseUpdateKind"/>.</param>
    [JsonConstructor]
    public SpeechToTextResponseUpdateKind(string value)
    {
        Value = Throw.IfNullOrWhitespace(value);
    }

    /// <summary>
    /// Returns a value indicating whether two <see cref="SpeechToTextResponseUpdateKind"/> instances are equivalent, as determined by a
    /// case-insensitive comparison of their values.
    /// </summary>
    /// <param name="left">The first <see cref="SpeechToTextResponseUpdateKind"/> instance to compare.</param>
    /// <param name="right">The second <see cref="SpeechToTextResponseUpdateKind"/> instance to compare.</param>
    /// <returns><see langword="true"/> if left and right are both null or have equivalent values; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(SpeechToTextResponseUpdateKind left, SpeechToTextResponseUpdateKind right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Returns a value indicating whether two <see cref="SpeechToTextResponseUpdateKind"/> instances are not equivalent, as determined by a
    /// case-insensitive comparison of their values.
    /// </summary>
    /// <param name="left">The first <see cref="SpeechToTextResponseUpdateKind"/> instance to compare. </param>
    /// <param name="right">The second <see cref="SpeechToTextResponseUpdateKind"/> instance to compare. </param>
    /// <returns><see langword="true"/> if left and right have different values; <see langword="false"/> if they have equivalent values or are both null.</returns>
    public static bool operator !=(SpeechToTextResponseUpdateKind left, SpeechToTextResponseUpdateKind right)
    {
        return !(left == right);
    }

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is SpeechToTextResponseUpdateKind otherRole && Equals(otherRole);

    /// <inheritdoc/>
    public bool Equals(SpeechToTextResponseUpdateKind other)
        => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public override int GetHashCode()
        => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    /// <inheritdoc/>
    public override string ToString() => Value;

    /// <summary>Provides a <see cref="JsonConverter{T}"/> for serializing <see cref="SpeechToTextResponseUpdateKind"/> instances.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class Converter : JsonConverter<SpeechToTextResponseUpdateKind>
    {
        /// <inheritdoc />
        public override SpeechToTextResponseUpdateKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetString()!);

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, SpeechToTextResponseUpdateKind value, JsonSerializerOptions options)
            => Throw.IfNull(writer).WriteStringValue(value.Value);
    }
}
