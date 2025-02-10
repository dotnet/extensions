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
/// Describes the intended purpose of a message within a chat interaction.
/// </summary>
[JsonConverter(typeof(Converter))]
public readonly struct ChatRole : IEquatable<ChatRole>
{
    /// <summary>Gets the role that instructs or sets the behavior of the system.</summary>
    public static ChatRole System { get; } = new("system");

    /// <summary>Gets the role that provides responses to system-instructed, user-prompted input.</summary>
    public static ChatRole Assistant { get; } = new("assistant");

    /// <summary>Gets the role that provides user input for chat interactions.</summary>
    public static ChatRole User { get; } = new("user");

    /// <summary>Gets the role that provides additional information and references in response to tool use requests.</summary>
    public static ChatRole Tool { get; } = new("tool");

    /// <summary>
    /// Gets the value associated with this <see cref="ChatRole"/>.
    /// </summary>
    /// <remarks>
    /// The value will be serialized into the "role" message field of the Chat Message format.
    /// </remarks>
    public string Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatRole"/> struct with the provided value.
    /// </summary>
    /// <param name="value">The value to associate with this <see cref="ChatRole"/>.</param>
    [JsonConstructor]
    public ChatRole(string value)
    {
        Value = Throw.IfNullOrWhitespace(value);
    }

    /// <summary>
    /// Returns a value indicating whether two <see cref="ChatRole"/> instances are equivalent, as determined by a
    /// case-insensitive comparison of their values.
    /// </summary>
    /// <param name="left">The first <see cref="ChatRole"/> instance to compare.</param>
    /// <param name="right">The second <see cref="ChatRole"/> instance to compare.</param>
    /// <returns><see langword="true"/> if left and right are both null or have equivalent values; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(ChatRole left, ChatRole right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Returns a value indicating whether two <see cref="ChatRole"/> instances are not equivalent, as determined by a
    /// case-insensitive comparison of their values.
    /// </summary>
    /// <param name="left">The first <see cref="ChatRole"/> instance to compare. </param>
    /// <param name="right">The second <see cref="ChatRole"/> instance to compare. </param>
    /// <returns><see langword="true"/> if left and right have different values; <see langword="false"/> if they have equivalent values or are both null.</returns>
    public static bool operator !=(ChatRole left, ChatRole right)
    {
        return !(left == right);
    }

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is ChatRole otherRole && Equals(otherRole);

    /// <inheritdoc/>
    public bool Equals(ChatRole other)
        => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public override int GetHashCode()
        => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    /// <inheritdoc/>
    public override string ToString() => Value;

    /// <summary>Provides a <see cref="JsonConverter{ChatRole}"/> for serializing <see cref="ChatRole"/> instances.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class Converter : JsonConverter<ChatRole>
    {
        /// <inheritdoc />
        public override ChatRole Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetString()!);

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, ChatRole value, JsonSerializerOptions options) =>
            Throw.IfNull(writer).WriteStringValue(value.Value);
    }
}
