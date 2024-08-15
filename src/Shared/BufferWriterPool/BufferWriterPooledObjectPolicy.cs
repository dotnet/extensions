// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Shared.Pools;

/// <summary>
/// An object pooling policy designed for <see cref="BufferWriter{T}"/>.
/// </summary>
/// <typeparam name="T">The type of objects to hold in the buffer writer.</typeparam>

#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif

internal sealed class BufferWriterPooledObjectPolicy<T> : PooledObjectPolicy<BufferWriter<T>>
{
    /// <summary>
    /// Default maximum retained capacity of buffer writer instances in the pool.
    /// </summary>
    private const int DefaultMaximumRetainedCapacity = 256 * 1024;

    /// <summary>
    /// Initializes a new instance of the <see cref="BufferWriterPooledObjectPolicy{T}"/> class.
    /// </summary>
    /// <param name="maximumRetainedCapacity">
    /// The maximum capacity of <see cref="BufferWriter{T}"/> to keep in the pool.
    /// If an object is returned to the pool whose capacity exceeds this number, the object
    /// instance is not added to the pool, and thus becomes eligible for garbage collection.
    /// </param>
    public BufferWriterPooledObjectPolicy(int maximumRetainedCapacity = DefaultMaximumRetainedCapacity)
    {
        MaximumRetainedCapacity = Throw.IfLessThan(maximumRetainedCapacity, 1);
    }

    /// <summary>
    /// Gets the maximum capacity of <see cref="BufferWriter{T}"/> to keep in the pool.
    /// </summary>
    /// <remarks>
    /// If an object is returned to the pool whose capacity exceeds this number, the object
    /// instance is not added to the pool, and thus becomes eligible for garbage collection.
    /// Default maximum retained capacity is 256 * 1024 bytes.
    /// </remarks>
    public int MaximumRetainedCapacity { get; }

    /// <summary>
    /// Creates an instance of <see cref="BufferWriter{T}"/>.
    /// </summary>
    /// <returns>The newly created instance.</returns>
    public override BufferWriter<T> Create() => new();

    /// <summary>
    /// Performs any work needed before returning an object to a pool.
    /// </summary>
    /// <param name="obj">The object to return to a pool.</param>
    /// <returns>true if the object should be returned to the pool, false if it shouldn't.</returns>
    public override bool Return(BufferWriter<T> obj)
    {
        _ = Throw.IfNull(obj);

        if (obj.Capacity > MaximumRetainedCapacity)
        {
            // Too big. Discard this one.
            return false;
        }

        obj.Reset();
        return true;
    }
}
