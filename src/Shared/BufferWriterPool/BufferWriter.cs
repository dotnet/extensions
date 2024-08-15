// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using Microsoft.Shared.Diagnostics;
#if NETCOREAPP3_1_OR_GREATER
using System.Runtime.CompilerServices;
#endif

namespace Microsoft.Shared.Pools;

/// <summary>
/// Represents an output sink into which <typeparamref name="T"/> data can be written.
/// </summary>
/// <remarks>
/// This class is similar to <c>System.Buffers.ArrayBufferWriter&lt;T&gt;</c>, with the exception that the
/// <c>ArrayBufferWriter&lt;T&gt;.Clear</c> method has been replaced with a <see cref="Reset"/>
/// method. When used with value types, <see cref="Reset"/> doesn't clear the underlying memory
/// buffer to <c>default(T)</c>, which makes it considerably faster. Additionally, this class
/// lets you explicitly set the capacity of the underlying buffer.
/// </remarks>
/// <typeparam name="T">The type of value that will be written into the writer.</typeparam>

#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif

internal sealed class BufferWriter<T> : IBufferWriter<T>
{
    internal const int MaxArrayLength = 0X7FEF_FFFF;   // Copy of the internal Array.MaxArrayLength const
    private const int DefaultCapacity = 256;

    private T[] _buffer = Array.Empty<T>();

    /// <summary>
    /// Gets the data written to the underlying buffer so far.
    /// </summary>
    /// <remarks>
    /// The returned value can become stale whenever the buffer is written to again. As such, you should
    /// always call this property in order to get a fresh value before reading from the buffer.
    /// </remarks>
    public ReadOnlyMemory<T> WrittenMemory => _buffer.AsMemory(0, WrittenCount);

    /// <summary>
    /// Gets the data written to the underlying buffer so far.
    /// </summary>
    /// <remarks>
    /// The returned span can become stale whenever the buffer is written to again. As such, you should
    /// always call this property in order to get a fresh span before reading from the buffer.
    /// </remarks>
    public ReadOnlySpan<T> WrittenSpan => _buffer.AsSpan(0, WrittenCount);

    /// <summary>
    /// Gets the amount of data written to the underlying buffer so far.
    /// </summary>
    public int WrittenCount { get; private set; }

    /// <summary>
    /// Gets or sets the total amount of space within the underlying buffer.
    /// </summary>
    /// <remarks>
    /// When reducing the capacity, the value of <see cref="WrittenCount" /> is clamped to the
    /// new capacity.
    /// </remarks>
    public int Capacity
    {
        get => _buffer.Length;

        set
        {
            _ = Throw.IfLessThan(value, 0);

            Array.Resize(ref _buffer, value);
            if (WrittenCount > value)
            {
                WrittenCount = value;
            }
        }
    }

    /// <summary>
    /// Clears the data written to the underlying buffer.
    /// </summary>
    /// <remarks>
    /// You must reset the <see cref="BufferWriter{T}"/> before trying to re-use it.
    /// If <typeparamref name="T"/> is or contains reference types, than this method
    /// will clear the underlying memory buffers to default(T) in order to ensure the
    /// GC is able to reclaim references correctly. If <typeparamref name="T"/>
    /// is a value type and only contains value types, then this method doesn't
    /// clear memory and so completes considerably faster.
    /// </remarks>
    public void Reset()
    {
#if NETCOREAPP3_1_OR_GREATER
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            _buffer.AsSpan(0, WrittenCount).Clear();
        }
#else
        _buffer.AsSpan(0, WrittenCount).Clear();
#endif

        WrittenCount = 0;
    }

    /// <summary>
    /// Notifies <see cref="IBufferWriter{T}"/> that <paramref name="count"/> amount of data was written to the output <see cref="Span{T}"/>/<see cref="Memory{T}"/>.
    /// </summary>
    /// <param name="count">The amount of data that has been consumed.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="count"/> is negative or when attempting to advance past the end of the underlying buffer.
    /// </exception>
    /// <remarks>
    /// You must request a new buffer after calling <see cref="Advance"/> to continue writing more data and cannot write to a previously acquired buffer.
    /// </remarks>
    public void Advance(int count)
    {
        _ = Throw.IfOutOfRange(count, 0, _buffer.Length - WrittenCount);

        WrittenCount += count;
    }

    /// <summary>
    /// Returns a <see cref="Memory{T}"/> to write to that is at least the requested length (specified by <paramref name="sizeHint"/>).
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sizeHint"/> is negative.
    /// </exception>
    /// <remarks>
    /// This will never return an empty <see cref="Memory{T}"/>.
    /// There is no guarantee that successive calls will return the same buffer or the same-sized buffer.
    /// You must request a new buffer after calling <see cref="Advance"/> to continue writing more data and cannot write to a previously acquired buffer.
    /// </remarks>
    /// <param name="sizeHint">THe minimum size of the returned buffer. If this is 0, returns a buffer with at least 1 byte available.</param>
    /// <returns>A block of memory that can be written to.</returns>
    public Memory<T> GetMemory(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return _buffer.AsMemory(WrittenCount);
    }

    /// <summary>
    /// Returns a <see cref="Span{T}"/> to write to that is at least the requested length (specified by <paramref name="sizeHint"/>).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="sizeHint"/> is negative.
    /// </exception>
    /// <remarks>
    /// This will never return an empty <see cref="Span{T}"/>.
    /// There is no guarantee that successive calls will return the same buffer or the same-sized buffer.
    /// You must request a new buffer after calling <see cref="Advance"/> to continue writing more data and cannot write to a previously acquired buffer.
    /// </remarks>
    /// <param name="sizeHint">THe minimum size of the returned buffer. If this is 0, returns a buffer with at least 1 byte available.</param>
    /// <returns>A block of memory that can be written to.</returns>
    public Span<T> GetSpan(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return _buffer.AsSpan(WrittenCount);
    }

    private void EnsureCapacity(int sizeHint)
    {
        if (sizeHint == 0)
        {
            sizeHint = 1;
        }

        var avail = _buffer.Length - WrittenCount;
        if (sizeHint > avail)
        {
            var targetCapacity = _buffer.Length == 0 ? DefaultCapacity : _buffer.Length * 2;
            if (targetCapacity - WrittenCount < sizeHint)
            {
                targetCapacity = WrittenCount + sizeHint;
            }

            if ((uint)targetCapacity > MaxArrayLength)
            {
                Throw.InvalidOperationException("Exceeded array capacity");
            }

            Array.Resize(ref _buffer, targetCapacity);
        }
        else
        {
            _ = Throw.IfLessThan(sizeHint, 0);
        }
    }
}
