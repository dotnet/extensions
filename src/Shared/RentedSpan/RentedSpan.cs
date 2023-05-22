// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;

#pragma warning disable CA1716
namespace Microsoft.Shared.Pools;
#pragma warning restore CA1716

/// <summary>
/// Represents a span that's potentially created over a rented array.
/// </summary>
/// <typeparam name="T">The type of objects held by the span.</typeparam>
/// <remarks>
/// This type is used to implement a common pattern to improve performance
/// when using temporary buffers. The pattern encourages the use of stack-based
/// buffers when possible, which eliminate the overhead of garbage collection of
/// buffers.
///
/// With this type, you make a speculative call to allocate a buffer. If the buffer
/// is too large to fix on the stack, it is allocated from <see cref="ArrayPool{T}"/>.
/// If the buffer could be allocated on the stack, then no buffer is acquired from the
/// array pool, and instead the caller is expected to use <see langword="stackalloc" />
/// to get the buffer.
/// </remarks>
/// <example>
/// <code>
/// using var rental = new RentedSpan&lt;char&gt;(length);
/// var span = rental.Rented ? rental.Span : stackalloc char[length];
/// </code>
/// </example>
#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif

internal readonly ref struct RentedSpan<T>
{
    /// <summary>
    /// The minimum size in bytes that triggers buffer rental.
    /// </summary>
    internal const int MinimumRentalSpace = 256;

    private readonly int _length;
    private readonly T[]? _rentedBuffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="RentedSpan{T}"/> struct.
    /// </summary>
    /// <param name="length">The desired length of the span. if this value is %lt;= 0, no buffer is allocated.</param>
    public RentedSpan(int length)
    {
        var size = Unsafe.SizeOf<T>() * length;
        if (size >= MinimumRentalSpace)
        {
            _rentedBuffer = ArrayPool<T>.Shared.Rent(length);
        }
        else
        {
            _rentedBuffer = null;
        }

        _length = length;
    }

    /// <summary>
    /// Returns a rented array back to the array pool.
    /// </summary>
    /// <remarks>
    /// If no array was actually allocated from the array pool
    /// (when <see cref="Rented"/> is <see langword="false" />),
    /// then this method has no effect.
    ///
    /// Calling this method multiple times on the same object is not supported, don't do it.
    /// </remarks>
    public void Dispose()
    {
        if (_rentedBuffer != null)
        {
            ArrayPool<T>.Shared.Return(_rentedBuffer);
        }
    }

    /// <summary>
    /// Gets a span over the rented buffer.
    /// </summary>
    /// <remarks>
    /// If no buffer was rented (because the buffer was deemed too small), then this returns an empty span.
    /// When a buffer isn't rented by this type, it's a cue to you to allocate buffer from the stack instead
    /// using stackalloc.
    /// </remarks>
    public Span<T> Span => _rentedBuffer != null ? _rentedBuffer.AsSpan(0, _length) : Array.Empty<T>().AsSpan();

    /// <summary>
    /// Gets a value indicating whether a buffer has been rented.
    /// </summary>
    public bool Rented => _rentedBuffer != null;
}
