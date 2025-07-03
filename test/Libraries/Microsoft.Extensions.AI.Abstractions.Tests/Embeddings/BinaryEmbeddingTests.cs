// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class BinaryEmbeddingTests
{
    [Fact]
    public void Ctor_Roundtrips()
    {
        BitArray vector = new BitArray(new bool[] { false, true, false, true });

        BinaryEmbedding e = new(vector);
        Assert.Same(vector, e.Vector);
        Assert.Null(e.ModelId);
        Assert.Null(e.CreatedAt);
        Assert.Null(e.AdditionalProperties);
    }

    [Fact]
    public void Properties_Roundtrips()
    {
        BitArray vector = new BitArray(new bool[] { false, true, false, true });

        BinaryEmbedding e = new(vector);

        Assert.Same(vector, e.Vector);
        BitArray newVector = new BitArray(new bool[] { true, false, true, false });
        e.Vector = newVector;
        Assert.Same(newVector, e.Vector);

        Assert.Null(e.ModelId);
        e.ModelId = "text-embedding-3-small";
        Assert.Equal("text-embedding-3-small", e.ModelId);

        Assert.Null(e.CreatedAt);
        DateTimeOffset createdAt = DateTimeOffset.Parse("2022-01-01T00:00:00Z");
        e.CreatedAt = createdAt;
        Assert.Equal(createdAt, e.CreatedAt);

        Assert.Null(e.AdditionalProperties);
        AdditionalPropertiesDictionary props = new();
        e.AdditionalProperties = props;
        Assert.Same(props, e.AdditionalProperties);
    }

    [Fact]
    public void Serialization_Roundtrips()
    {
        foreach (int length in Enumerable.Range(0, 64).Concat(new[] { 10_000 }))
        {
            bool[] bools = new bool[length];
            Random r = new(42);
            for (int i = 0; i < length; i++)
            {
                bools[i] = r.Next(2) != 0;
            }

            BitArray vector = new BitArray(bools);
            BinaryEmbedding e = new(vector);

            string json = JsonSerializer.Serialize(e, TestJsonSerializerContext.Default.Embedding);
            Assert.Equal($$"""{"$type":"binary","vector":"{{string.Concat(vector.Cast<bool>().Select(b => b ? '1' : '0'))}}"}""", json);

            BinaryEmbedding result = Assert.IsType<BinaryEmbedding>(JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.Embedding));
            Assert.Equal(e.Vector, result.Vector);
        }
    }

    [Fact]
    public void Derialization_SupportsEncodedBits()
    {
        BinaryEmbedding result = Assert.IsType<BinaryEmbedding>(JsonSerializer.Deserialize(
            """{"$type":"binary","vector":"\u0030\u0031\u0030\u0031\u0030\u0031"}""",
            TestJsonSerializerContext.Default.Embedding));

        Assert.Equal(new BitArray(new[] { false, true, false, true, false, true }), result.Vector);
    }

    [Theory]
    [InlineData("""{"$type":"binary","vector":"\u0030\u0032"}""")]
    [InlineData("""{"$type":"binary","vector":"02"}""")]
    [InlineData("""{"$type":"binary","vector":" "}""")]
    [InlineData("""{"$type":"binary","vector":10101}""")]
    public void Derialization_InvalidBinaryEmbedding_Throws(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.Embedding));
    }
}
