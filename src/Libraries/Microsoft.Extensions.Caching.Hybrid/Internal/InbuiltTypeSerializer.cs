// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;

#if !NET5_0_OR_GREATER
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
#endif

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

internal sealed class InbuiltTypeSerializer : IHybridCacheSerializer<string>, IHybridCacheSerializer<byte[]>
{
    public static InbuiltTypeSerializer Instance { get; } = new();

    string IHybridCacheSerializer<string>.Deserialize(ReadOnlySequence<byte> source)
        => DeserializeString(source);

    void IHybridCacheSerializer<string>.Serialize(string value, IBufferWriter<byte> target)
        => SerializeString(value, target);

    byte[] IHybridCacheSerializer<byte[]>.Deserialize(ReadOnlySequence<byte> source)
    => source.ToArray();

    void IHybridCacheSerializer<byte[]>.Serialize(byte[] value, IBufferWriter<byte> target)
        => target.Write(value);

    internal static string DeserializeString(ReadOnlySequence<byte> source)
    {
#if NET5_0_OR_GREATER
        return Encoding.UTF8.GetString(source);
#else
        if (source.IsSingleSegment && MemoryMarshal.TryGetArray(source.First, out var segment))
        {
            // we can use the existing single chunk as-is
            return Encoding.UTF8.GetString(segment.Array, segment.Offset, segment.Count);
        }

        var length = checked((int)source.Length);
        var oversized = ArrayPool<byte>.Shared.Rent(length);
        source.CopyTo(oversized);
        var s = Encoding.UTF8.GetString(oversized, 0, length);
        ArrayPool<byte>.Shared.Return(oversized);
        return s;
#endif
    }

    internal static void SerializeString(string value, IBufferWriter<byte> target)
    {
#if NET5_0_OR_GREATER
        Encoding.UTF8.GetBytes(value, target);
#else
        var length = Encoding.UTF8.GetByteCount(value);
        var oversized = ArrayPool<byte>.Shared.Rent(length);
        var actual = Encoding.UTF8.GetBytes(value, 0, value.Length, oversized, 0);
        Debug.Assert(actual == length, "encoding length mismatch");
        target.Write(new(oversized, 0, length));
        ArrayPool<byte>.Shared.Return(oversized);
#endif
    }
}
