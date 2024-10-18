// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the reason a chat response completed.</summary>
[JsonConverter(typeof(Converter))]
public readonly struct ChatFinishReason : IEquatable<ChatFinishReason>
{
    /// <summary>The finish reason value. If null because `default(ChatFinishReason)` was used, the instance will behave like <see cref="Stop"/>.</summary>
    private readonly string? _value;

    /// <summary>Initializes a new instance of the <see cref="ChatFinishReason"/> struct with a string that describes the reason.</summary>
    /// <param name="value">The reason value.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> is empty or composed entirely of whitespace.</exception>
    [JsonConstructor]
    public ChatFinishReason(string value)
    {
        _value = Throw.IfNullOrWhitespace(value);
    }

    /// <summary>Gets the finish reason value.</summary>
    public string Value => _value ?? Stop.Value;

    /// <inheritdoc />
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is ChatFinishReason other && Equals(other);

    /// <inheritdoc />
    public bool Equals(ChatFinishReason other) => StringComparer.OrdinalIgnoreCase.Equals(Value, other.Value);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    /// <summary>
    /// Compares two instances.
    /// </summary>
    /// <param name="left">Left argument of the comparison.</param>
    /// <param name="right">Right argument of the comparison.</param>
    /// <returns><see langword="true" /> when equal, <see langword="false" /> otherwise.</returns>
    public static bool operator ==(ChatFinishReason left, ChatFinishReason right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two instances.
    /// </summary>
    /// <param name="left">Left argument of the comparison.</param>
    /// <param name="right">Right argument of the comparison.</param>
    /// <returns><see langword="true" /> when not equal, <see langword="false" /> otherwise.</returns>
    public static bool operator !=(ChatFinishReason left, ChatFinishReason right)
    {
        return !(left == right);
    }

    /// <summary>Gets the <see cref="Value"/> of the finish reason.</summary>
    /// <returns>The <see cref="Value"/> of the finish reason.</returns>
    public override string ToString() => Value;

    /// <summary>Gets a <see cref="ChatFinishReason"/> representing the model encountering a natural stop point or provided stop sequence.</summary>
    public static ChatFinishReason Stop { get; } = new("stop");

    /// <summary>Gets a <see cref="ChatFinishReason"/> representing the model reaching the maximum length allowed for the request and/or response (typically in terms of tokens).</summary>
    public static ChatFinishReason Length { get; } = new("length");

    /// <summary>Gets a <see cref="ChatFinishReason"/> representing the model requesting the use of a tool that was defined in the request.</summary>
    public static ChatFinishReason ToolCalls { get; } = new("tool_calls");

    /// <summary>Gets a <see cref="ChatFinishReason"/> representing the model filtering content, whether for safety, prohibited content, sensitive content, or other such issues.</summary>
    public static ChatFinishReason ContentFilter { get; } = new("content_filter");

    /// <summary>Provides a <see cref="JsonConverter{ChatFinishReason}"/> for serializing <see cref="ChatRole"/> instances.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class Converter : JsonConverter<ChatFinishReason>
    {
        /// <inheritdoc/>
        public override ChatFinishReason Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetString()!);

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, ChatFinishReason value, JsonSerializerOptions options) =>
            Throw.IfNull(writer).WriteStringValue(value.Value);
    }
}
