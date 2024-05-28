// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.ObjectPool;

#pragma warning disable CA1716
namespace Microsoft.Shared.Pools;
#pragma warning restore CA1716

/// <summary>
///  Represents a buffer writer that can be automatically returned to an object pool upon dispose.
/// </summary>
/// <typeparam name="T">The type of the elements in the buffer.</typeparam>
#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
internal readonly struct ReturnableBufferWriter<T> : IDisposable
{
    private readonly ObjectPool<BufferWriter<T>> _pool;

    /// <summary>
    ///  Initializes a new instance of the <see cref="ReturnableBufferWriter{T}"/> struct.
    /// </summary>
    /// <param name="pool">The object pool to return the buffer writer to.</param>
    public ReturnableBufferWriter(ObjectPool<BufferWriter<T>> pool)
    {
        _pool = pool;
        Buffer = pool.Get();
    }

    /// <summary>
    ///  Gets the buffer writer.
    /// </summary>
    public BufferWriter<T> Buffer { get; }

    /// <summary>
    ///  Disposes the buffer writer and returns it to the object pool.
    /// </summary>
    public readonly void Dispose()
    {
        Buffer.Reset();
        _pool.Return(Buffer);
    }
}
