// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Diagnostics;

#pragma warning disable CA1716
namespace Microsoft.Shared.Pools;
#pragma warning restore CA1716

#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
internal static class BufferWriterPool
{
    internal const int DefaultCapacity = 1024;
    private const int DefaultMaxBufferWriterCapacity = 256 * 1024;

    /// <summary>
    /// Creates an object pool of <see cref="BufferWriter{T}"/> instances.
    /// </summary>
    /// <typeparam name="T">The type of object managed by the buffer writers.</typeparam>
    /// <param name="maxCapacity">The maximum number of items to keep in the pool. This defaults to 1024. This value is a recommendation, the pool may keep more objects than this.</param>
    /// <param name="maxBufferWriterCapacity">The maximum capacity of the buffer writers to keep in the pool. This defaults to 256K.</param>
    /// <returns>The pool.</returns>
    public static ObjectPool<BufferWriter<T>> CreateBufferWriterPool<T>(int maxCapacity = DefaultCapacity, int maxBufferWriterCapacity = DefaultMaxBufferWriterCapacity)
    {
        _ = Throw.IfLessThan(maxCapacity, 1);
        _ = Throw.IfLessThan(maxBufferWriterCapacity, 1);

        return PoolFactory.CreatePool<BufferWriter<T>>(new BufferWriterPooledObjectPolicy<T>(maxBufferWriterCapacity), maxCapacity);
    }

    /// <summary>
    /// Gets the shared pool of <see cref="BufferWriter{T}"/> instances.
    /// </summary>
    public static ObjectPool<BufferWriter<byte>> SharedBufferWriterPool { get; } = CreateBufferWriterPool<byte>();
}
