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
/// Describes the intended purpose of a specific update during streaming of audio transcriptions.
/// </summary>
[JsonConverter(typeof(Converter))]
public readonly struct AudioTranscriptionResponseUpdateKind : IEquatable<AudioTranscriptionResponseUpdateKind>
{
    /// <summary>Gets when the transcription session is opened.</summary>
    public static AudioTranscriptionResponseUpdateKind SessionOpen { get; } = new("sessionopen");

    /// <summary>Gets when a non-blocking error occurs during transcription.</summary>
    public static AudioTranscriptionResponseUpdateKind Error { get; } = new("error");

    /// <summary>Gets when the transcription is in progress, without waiting for silence.</summary>
    public static AudioTranscriptionResponseUpdateKind Transcribing { get; } = new("transcribing");

    /// <summary>Gets when the transcription is complete after small period of silence.</summary>
    public static AudioTranscriptionResponseUpdateKind Transcribed { get; } = new("transcribed");

    /// <summary>Gets when the transcription session is closed.</summary>
    public static AudioTranscriptionResponseUpdateKind SessionClose { get; } = new("sessionclose");

    /// <summary>
    /// Gets the value associated with this <see cref="AudioTranscriptionResponseUpdateKind"/>.
    /// </summary>
    /// <remarks>
    /// The value will be serialized into the "kind" message field of the Audio Transcription Update format.
    /// </remarks>
    public string Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioTranscriptionResponseUpdateKind"/> struct with the provided value.
    /// </summary>
    /// <param name="value">The value to associate with this <see cref="AudioTranscriptionResponseUpdateKind"/>.</param>
    [JsonConstructor]
    public AudioTranscriptionResponseUpdateKind(string value)
    {
        Value = Throw.IfNullOrWhitespace(value);
    }

    /// <summary>
    /// Returns a value indicating whether two <see cref="AudioTranscriptionResponseUpdateKind"/> instances are equivalent, as determined by a
    /// case-insensitive comparison of their values.
    /// </summary>
    /// <param name="left">The first <see cref="AudioTranscriptionResponseUpdateKind"/> instance to compare.</param>
    /// <param name="right">The second <see cref="AudioTranscriptionResponseUpdateKind"/> instance to compare.</param>
    /// <returns><see langword="true"/> if left and right are both null or have equivalent values; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(AudioTranscriptionResponseUpdateKind left, AudioTranscriptionResponseUpdateKind right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Returns a value indicating whether two <see cref="AudioTranscriptionResponseUpdateKind"/> instances are not equivalent, as determined by a
    /// case-insensitive comparison of their values.
    /// </summary>
    /// <param name="left">The first <see cref="AudioTranscriptionResponseUpdateKind"/> instance to compare. </param>
    /// <param name="right">The second <see cref="AudioTranscriptionResponseUpdateKind"/> instance to compare. </param>
    /// <returns><see langword="true"/> if left and right have different values; <see langword="false"/> if they have equivalent values or are both null.</returns>
    public static bool operator !=(AudioTranscriptionResponseUpdateKind left, AudioTranscriptionResponseUpdateKind right)
    {
        return !(left == right);
    }

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is AudioTranscriptionResponseUpdateKind otherRole && Equals(otherRole);

    /// <inheritdoc/>
    public bool Equals(AudioTranscriptionResponseUpdateKind other)
        => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public override int GetHashCode()
        => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    /// <inheritdoc/>
    public override string ToString() => Value;

    /// <summary>Provides a <see cref="JsonConverter{AudioTranscriptionResponseUpdateKind}"/> for serializing <see cref="AudioTranscriptionResponseUpdateKind"/> instances.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class Converter : JsonConverter<AudioTranscriptionResponseUpdateKind>
    {
        /// <inheritdoc />
        public override AudioTranscriptionResponseUpdateKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetString()!);

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, AudioTranscriptionResponseUpdateKind value, JsonSerializerOptions options)
            => Throw.IfNull(writer).WriteStringValue(value.Value);
    }
}
