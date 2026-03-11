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
/// Represents the type of a real-time server message.
/// This is used to identify the message type being received from the model.
/// </summary>
/// <remarks>
/// <para>
/// Well-known message types are provided as static properties. Providers may define additional
/// message types by constructing new instances with custom values.
/// </para>
/// <para>
/// Provider implementations that want to support the built-in middleware pipeline
/// (<see langword="FunctionInvokingRealtimeClientSession"/> and
/// <see langword="OpenTelemetryRealtimeClientSession"/>) must emit the following
/// message types at appropriate points during response generation:
/// <list type="bullet">
/// <item><see cref="ResponseCreated"/> — when the model begins generating a new response.</item>
/// <item><see cref="ResponseDone"/> — when the model has finished generating a response (with usage data if available).</item>
/// <item><see cref="ResponseOutputItemAdded"/> — when a new output item (e.g., function call, message) is added during response generation.</item>
/// <item><see cref="ResponseOutputItemDone"/> — when an individual output item has completed. This is required for function invocation middleware to detect and invoke tool calls.</item>
/// </list>
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIRealTime, UrlFormat = DiagnosticIds.UrlFormat)]
[JsonConverter(typeof(Converter))]
[DebuggerDisplay("{Value,nq}")]
public readonly struct RealtimeServerMessageType : IEquatable<RealtimeServerMessageType>
{
    /// <summary>Gets a message type indicating that the response contains only raw content.</summary>
    /// <remarks>
    /// This type supports extensibility for custom content types not natively supported by the SDK.
    /// </remarks>
    public static RealtimeServerMessageType RawContentOnly { get; } = new("RawContentOnly");

    /// <summary>Gets a message type indicating the output of audio transcription for user audio written to the user audio buffer.</summary>
    public static RealtimeServerMessageType InputAudioTranscriptionCompleted { get; } = new("InputAudioTranscriptionCompleted");

    /// <summary>Gets a message type indicating the text value of an input audio transcription content part is updated with incremental transcription results.</summary>
    public static RealtimeServerMessageType InputAudioTranscriptionDelta { get; } = new("InputAudioTranscriptionDelta");

    /// <summary>Gets a message type indicating that the audio transcription for user audio written to the user audio buffer has failed.</summary>
    public static RealtimeServerMessageType InputAudioTranscriptionFailed { get; } = new("InputAudioTranscriptionFailed");

    /// <summary>Gets a message type indicating the output text update with incremental results.</summary>
    public static RealtimeServerMessageType OutputTextDelta { get; } = new("OutputTextDelta");

    /// <summary>Gets a message type indicating the output text is complete.</summary>
    public static RealtimeServerMessageType OutputTextDone { get; } = new("OutputTextDone");

    /// <summary>Gets a message type indicating the model-generated transcription of audio output updated.</summary>
    public static RealtimeServerMessageType OutputAudioTranscriptionDelta { get; } = new("OutputAudioTranscriptionDelta");

    /// <summary>Gets a message type indicating the model-generated transcription of audio output is done streaming.</summary>
    public static RealtimeServerMessageType OutputAudioTranscriptionDone { get; } = new("OutputAudioTranscriptionDone");

    /// <summary>Gets a message type indicating the audio output updated.</summary>
    public static RealtimeServerMessageType OutputAudioDelta { get; } = new("OutputAudioDelta");

    /// <summary>Gets a message type indicating the audio output is done streaming.</summary>
    public static RealtimeServerMessageType OutputAudioDone { get; } = new("OutputAudioDone");

    /// <summary>Gets a message type indicating the response has completed.</summary>
    public static RealtimeServerMessageType ResponseDone { get; } = new("ResponseDone");

    /// <summary>Gets a message type indicating the response has been created.</summary>
    public static RealtimeServerMessageType ResponseCreated { get; } = new("ResponseCreated");

    /// <summary>Gets a message type indicating an individual output item in the response has completed.</summary>
    public static RealtimeServerMessageType ResponseOutputItemDone { get; } = new("ResponseOutputItemDone");

    /// <summary>Gets a message type indicating an individual output item has been added to the response.</summary>
    public static RealtimeServerMessageType ResponseOutputItemAdded { get; } = new("ResponseOutputItemAdded");

    /// <summary>Gets a message type indicating a conversation item has been added.</summary>
    public static RealtimeServerMessageType ConversationItemAdded { get; } = new("ConversationItemAdded");

    /// <summary>Gets a message type indicating a conversation item is complete.</summary>
    public static RealtimeServerMessageType ConversationItemDone { get; } = new("ConversationItemDone");

    /// <summary>Gets a message type indicating an error occurred while processing the request.</summary>
    public static RealtimeServerMessageType Error { get; } = new("Error");

    /// <summary>
    /// Gets the value associated with this <see cref="RealtimeServerMessageType"/>.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RealtimeServerMessageType"/> struct with the provided value.
    /// </summary>
    /// <param name="value">The value to associate with this <see cref="RealtimeServerMessageType"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/> or whitespace.</exception>
    [JsonConstructor]
    public RealtimeServerMessageType(string value)
    {
        Value = Throw.IfNullOrWhitespace(value);
    }

    /// <summary>
    /// Returns a value indicating whether two <see cref="RealtimeServerMessageType"/> instances are equivalent, as determined by a
    /// case-insensitive comparison of their values.
    /// </summary>
    /// <param name="left">The first instance to compare.</param>
    /// <param name="right">The second instance to compare.</param>
    /// <returns><see langword="true"/> if left and right have equivalent values; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(RealtimeServerMessageType left, RealtimeServerMessageType right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Returns a value indicating whether two <see cref="RealtimeServerMessageType"/> instances are not equivalent, as determined by a
    /// case-insensitive comparison of their values.
    /// </summary>
    /// <param name="left">The first instance to compare.</param>
    /// <param name="right">The second instance to compare.</param>
    /// <returns><see langword="true"/> if left and right have different values; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(RealtimeServerMessageType left, RealtimeServerMessageType right)
    {
        return !(left == right);
    }

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is RealtimeServerMessageType other && Equals(other);

    /// <inheritdoc/>
    public bool Equals(RealtimeServerMessageType other)
        => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public override int GetHashCode()
        => Value is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    /// <inheritdoc/>
    public override string ToString() => Value ?? string.Empty;

    /// <summary>Provides a <see cref="JsonConverter{RealtimeServerMessageType}"/> for serializing <see cref="RealtimeServerMessageType"/> instances.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class Converter : JsonConverter<RealtimeServerMessageType>
    {
        /// <inheritdoc />
        public override RealtimeServerMessageType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetString()!);

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, RealtimeServerMessageType value, JsonSerializerOptions options) =>
            Throw.IfNull(writer).WriteStringValue(value.Value);
    }
}
