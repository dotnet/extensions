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

/// <summary>
/// Describes the intended purpose of a specific update during streaming of text to speech updates.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AITextToSpeech, UrlFormat = DiagnosticIds.UrlFormat)]
[JsonConverter(typeof(Converter))]
public readonly struct TextToSpeechResponseUpdateKind : IEquatable<TextToSpeechResponseUpdateKind>
{
    /// <summary>Gets when the generated audio speech session is opened.</summary>
    public static TextToSpeechResponseUpdateKind SessionOpen { get; } = new("sessionopen");

    /// <summary>Gets when a non-blocking error occurs during text to speech updates.</summary>
    public static TextToSpeechResponseUpdateKind Error { get; } = new("error");

    /// <summary>Gets when the audio update is in progress.</summary>
    public static TextToSpeechResponseUpdateKind AudioUpdating { get; } = new("audioupdating");

    /// <summary>Gets when an audio chunk has been fully generated.</summary>
    public static TextToSpeechResponseUpdateKind AudioUpdated { get; } = new("audioupdated");

    /// <summary>Gets when the generated audio speech session is closed.</summary>
    public static TextToSpeechResponseUpdateKind SessionClose { get; } = new("sessionclose");

    /// <summary>
    /// Gets the value associated with this <see cref="TextToSpeechResponseUpdateKind"/>.
    /// </summary>
    /// <remarks>
    /// The value will be serialized into the "kind" message field of the text to speech update format.
    /// </remarks>
    public string Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextToSpeechResponseUpdateKind"/> struct with the provided value.
    /// </summary>
    /// <param name="value">The value to associate with this <see cref="TextToSpeechResponseUpdateKind"/>.</param>
    [JsonConstructor]
    public TextToSpeechResponseUpdateKind(string value)
    {
        Value = Throw.IfNullOrWhitespace(value);
    }

    /// <summary>
    /// Returns a value indicating whether two <see cref="TextToSpeechResponseUpdateKind"/> instances are equivalent, as determined by a
    /// case-insensitive comparison of their values.
    /// </summary>
    /// <param name="left">The first <see cref="TextToSpeechResponseUpdateKind"/> instance to compare.</param>
    /// <param name="right">The second <see cref="TextToSpeechResponseUpdateKind"/> instance to compare.</param>
    /// <returns><see langword="true"/> if left and right are both null or have equivalent values; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(TextToSpeechResponseUpdateKind left, TextToSpeechResponseUpdateKind right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Returns a value indicating whether two <see cref="TextToSpeechResponseUpdateKind"/> instances are not equivalent, as determined by a
    /// case-insensitive comparison of their values.
    /// </summary>
    /// <param name="left">The first <see cref="TextToSpeechResponseUpdateKind"/> instance to compare. </param>
    /// <param name="right">The second <see cref="TextToSpeechResponseUpdateKind"/> instance to compare. </param>
    /// <returns><see langword="true"/> if left and right have different values; <see langword="false"/> if they have equivalent values or are both null.</returns>
    public static bool operator !=(TextToSpeechResponseUpdateKind left, TextToSpeechResponseUpdateKind right)
    {
        return !(left == right);
    }

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is TextToSpeechResponseUpdateKind otherKind && Equals(otherKind);

    /// <inheritdoc/>
    public bool Equals(TextToSpeechResponseUpdateKind other)
        => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public override int GetHashCode()
        => Value is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    /// <inheritdoc/>
    public override string ToString() => Value;

    /// <summary>Provides a <see cref="JsonConverter{T}"/> for serializing <see cref="TextToSpeechResponseUpdateKind"/> instances.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class Converter : JsonConverter<TextToSpeechResponseUpdateKind>
    {
        /// <inheritdoc />
        public override TextToSpeechResponseUpdateKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetString()!);

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, TextToSpeechResponseUpdateKind value, JsonSerializerOptions options)
            => Throw.IfNull(writer).WriteStringValue(value.Value);
    }
}
