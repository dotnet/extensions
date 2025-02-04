// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Microsoft.Extensions.Caching.Hybrid.Internal;

namespace Microsoft.Extensions.Caching.Hybrid.Tests;

public class SerializerTests
{
    [Fact]
    public void RoundTripString()
    {
        IHybridCacheSerializer<string> serializer = InbuiltTypeSerializer.Instance;

        using var target = RecyclableArrayBufferWriter<byte>.Create(int.MaxValue);
        serializer.Serialize("test value", target);
        Assert.True("test value"u8.SequenceEqual(target.GetCommittedMemory().Span));
        Assert.Equal("test value", serializer.Deserialize(target.AsSequence()));

        // and deserialize with multi-chunk
        Assert.Equal("test value", serializer.Deserialize(Split(target.AsSequence())));
    }

    [Fact]
    public void RoundTripByteArray()
    {
        IHybridCacheSerializer<byte[]> serializer = InbuiltTypeSerializer.Instance;
        var value = "test value"u8.ToArray();
        using var target = RecyclableArrayBufferWriter<byte>.Create(int.MaxValue);
        serializer.Serialize(value, target);
        Assert.True("test value"u8.SequenceEqual(target.GetCommittedMemory().Span));
        Assert.Equal(value, serializer.Deserialize(target.AsSequence()));

        // and deserialize with multi-chunk
        Assert.Equal(value, serializer.Deserialize(Split(target.AsSequence())));
    }

    private static ReadOnlySequence<byte> Split(ReadOnlySequence<byte> value)
    {
        // ensure the value is a multi-chunk segment
        if (!value.IsSingleSegment || value.Length <= 1)
        {
            // already multiple chunks, or cannot be split
            return value;
        }

        var chunk = value.First; // actually, single

        Segment first = new(chunk.Slice(0, 1), null);
        Segment second = new(chunk.Slice(1), first);
        var result = new ReadOnlySequence<byte>(first, 0, second, chunk.Length - 1);
        Assert.False(result.IsSingleSegment, "should be multi-segment");
        return result;
    }

    private sealed class Segment : ReadOnlySequenceSegment<byte>
    {
        public Segment(ReadOnlyMemory<byte> memory, Segment? previous)
        {
            if (previous is not null)
            {
                RunningIndex = previous.RunningIndex + previous.Memory.Length;
                previous.Next = this;
            }

            Memory = memory;
        }
    }

}
