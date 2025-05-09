// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents an embedding composed of a bit vector.</summary>
public sealed class BinaryEmbedding : Embedding
{
    /// <summary>The embedding vector this embedding represents.</summary>
    private BitArray _vector;

    /// <summary>Initializes a new instance of the <see cref="BinaryEmbedding"/> class with the embedding vector.</summary>
    /// <param name="vector">The embedding vector this embedding represents.</param>
    /// <exception cref="ArgumentNullException"><paramref name="vector"/> is <see langword="null"/>.</exception>
    public BinaryEmbedding(BitArray vector)
    {
        _vector = Throw.IfNull(vector);
    }

    /// <summary>Gets or sets the embedding vector this embedding represents.</summary>
    [JsonConverter(typeof(VectorConverter))]
    public BitArray Vector
    {
        get => _vector;
        set => _vector = Throw.IfNull(value);
    }

    /// <inheritdoc />
    [JsonIgnore]
    public override int Dimensions => _vector.Length;

    /// <summary>Provides a <see cref="JsonConverter{BitArray}"/> for serializing <see cref="BitArray"/> instances.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class VectorConverter : JsonConverter<BitArray>
    {
        /// <inheritdoc/>
        public override BitArray Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            _ = Throw.IfNull(typeToConvert);
            _ = Throw.IfNull(options);

            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Expected string property.");
            }

            ReadOnlySpan<byte> utf8;
            byte[]? tmpArray = null;
            if (!reader.HasValueSequence && !reader.ValueIsEscaped)
            {
                utf8 = reader.ValueSpan;
            }
            else
            {
                // This path should be rare.
                int length = reader.HasValueSequence ? checked((int)reader.ValueSequence.Length) : reader.ValueSpan.Length;
                tmpArray = ArrayPool<byte>.Shared.Rent(length);
                utf8 = tmpArray.AsSpan(0, reader.CopyString(tmpArray));
            }

            BitArray result = new(utf8.Length);

            for (int i = 0; i < utf8.Length; i++)
            {
                result[i] = utf8[i] switch
                {
                    (byte)'0' => false,
                    (byte)'1' => true,
                    _ => throw new JsonException("Expected binary character sequence.")
                };
            }

            if (tmpArray is not null)
            {
                ArrayPool<byte>.Shared.Return(tmpArray);
            }

            return result;
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, BitArray value, JsonSerializerOptions options)
        {
            _ = Throw.IfNull(writer);
            _ = Throw.IfNull(value);
            _ = Throw.IfNull(options);

            int length = value.Length;

            byte[] tmpArray = ArrayPool<byte>.Shared.Rent(length);

            Span<byte> utf8 = tmpArray.AsSpan(0, length);
            for (int i = 0; i < utf8.Length; i++)
            {
                utf8[i] = value[i] ? (byte)'1' : (byte)'0';
            }

            writer.WriteStringValue(utf8);

            ArrayPool<byte>.Shared.Return(tmpArray);
        }
    }
}
