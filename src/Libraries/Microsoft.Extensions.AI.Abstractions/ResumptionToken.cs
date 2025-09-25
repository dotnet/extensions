// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a token used to resume, continue, or rehydrate an operation across multiple scenarios/calls,
/// such as resuming a streamed response from a specific point or retrieving the result of a background operation.
/// Subclasses of this class encapsulate all necessary information within the token to facilitate these actions.
/// </summary>
[JsonConverter(typeof(Converter))]
public class ResumptionToken
{
    /// <summary>Bytes representing this token.</summary>
    private readonly byte[]? _bytes;

    /// <summary>Initializes a new instance of the <see cref="ResumptionToken"/> class.</summary>
    protected ResumptionToken()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ResumptionToken"/> class.</summary>
    /// <param name="bytes">Bytes to create the token from.</param>
    protected ResumptionToken(byte[] bytes)
    {
        _ = Throw.IfNull(bytes);

        _bytes = bytes;
    }

    /// <summary>Create a new instance of <see cref="ResumptionToken"/> from the provided <paramref name="bytes"/>.
    /// </summary>
    /// <param name="bytes">Bytes obtained from calling <see cref="ToBytes"/> on a <see cref="ResumptionToken"/>.</param>
    /// <returns>A <see cref="ResumptionToken"/> equivalent to the one from which
    /// the original<see cref="ResumptionToken"/> bytes were obtained.</returns>
    public static ResumptionToken FromBytes(byte[] bytes) => new(bytes);

    /// <summary>Gets the bytes representing this <see cref="ResumptionToken"/>.</summary>
    /// <returns>The bytes of this <see cref="ResumptionToken"/>.</returns>
    public virtual byte[] ToBytes() => _bytes ?? throw new InvalidOperationException("Unable to obtain this token's bytes.");

    /// <summary>Provides a <see cref="JsonConverter{ResumptionToken}"/> for serializing <see cref="ResumptionToken"/> instances.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class Converter : JsonConverter<ResumptionToken>
    {
        /// <inheritdoc/>
        public override ResumptionToken Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return ResumptionToken.FromBytes(reader.GetBytesFromBase64());
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, ResumptionToken value, JsonSerializerOptions options)
        {
            _ = Throw.IfNull(writer);
            _ = Throw.IfNull(value);

            writer.WriteBase64StringValue(value.ToBytes());
        }
    }
}
