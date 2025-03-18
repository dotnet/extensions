// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Utility class to stream <see cref="IAsyncEnumerable{T}"/> data content as a <see cref="Stream"/>.
/// </summary>
#if !NET8_0_OR_GREATER
internal sealed class DataContentAsyncEnumerableStream : Stream, IAsyncDisposable
#else
internal sealed class DataContentAsyncEnumerableStream : Stream
#endif
{
    private readonly IAsyncEnumerator<DataContent> _enumerator;
    private bool _isCompleted;
    private ReadOnlyMemory<byte>? _remainingData;
    private int _remainingDataOffset;
    private long _position;
    private DataContent? _firstDataContent;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataContentAsyncEnumerableStream"/> class/>.
    /// </summary>
    /// <param name="dataAsyncEnumerable">The async enumerable to stream.</param>
    /// <param name="firstDataContent">The first chunk of data to reconsider when reading.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <remarks>
    /// <paramref name="firstDataContent"/> needs to be considered back in the stream if <paramref name="dataAsyncEnumerable"/> was iterated before creating the stream.
    /// This can happen to check if the first enumerable item contains data or is just a reference only content.
    /// </remarks>
    internal DataContentAsyncEnumerableStream(IAsyncEnumerable<DataContent> dataAsyncEnumerable, DataContent? firstDataContent = null, CancellationToken cancellationToken = default)
    {
        _enumerator = Throw.IfNull(dataAsyncEnumerable).GetAsyncEnumerator(cancellationToken);
        _remainingData = Memory<byte>.Empty;
        _remainingDataOffset = 0;
        _position = 0;
        _firstDataContent = firstDataContent;
    }

    /// <inheritdoc/>
    public override bool CanRead => true;

    /// <inheritdoc/>
    public override bool CanSeek => false;

    /// <inheritdoc/>
    public override bool CanWrite => false;

    /// <inheritdoc/>
    public override long Length => throw new NotSupportedException();

    /// <inheritdoc/>
    public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(destination);

        byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            int bytesRead;
            while ((bytesRead = await EnumeratorReadAsync(new Memory<byte>(buffer), cancellationToken).ConfigureAwait(false)) != 0)
            {
#if NET
                await destination.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, bytesRead), cancellationToken).ConfigureAwait(false);
#else
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
#endif
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <inheritdoc/>
    public override long Position
    {
        get => _position;
        set => throw new NotSupportedException();
    }

    /// <inheritdoc/>
    public override void Flush() => throw new NotSupportedException();

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin) =>
        throw new NotSupportedException();

    /// <inheritdoc/>
    public override void SetLength(long value) =>
        throw new NotSupportedException();

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException("Use ReadAsync instead for asynchronous reading.");
    }

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => EnumeratorReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();

#if NET8_0_OR_GREATER
    /// <inheritdoc/>
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => EnumeratorReadAsync(buffer, cancellationToken);

    /// <inheritdoc/>
    public override async ValueTask DisposeAsync()
    {
        await _enumerator.DisposeAsync().ConfigureAwait(false);

        await base.DisposeAsync().ConfigureAwait(false);
    }
#else
    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _enumerator.DisposeAsync().ConfigureAwait(false);
    }

#pragma warning disable SA1202 // "protected" methods should come before "private" members
#pragma warning disable VSTHRD002 // Synchrnously waiting on tasks or awaiters may cause deadlocks.
    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        _enumerator.DisposeAsync().AsTask().GetAwaiter().GetResult();

        base.Dispose(disposing);
    }
#pragma warning restore SA1202 // "protected" methods should come before "private" members
#pragma warning restore VSTHRD002

#endif

    private async ValueTask<int> EnumeratorReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        if (_isCompleted)
        {
            return 0;
        }

        int bytesRead = 0;
        int totalToRead = buffer.Length;

        while (bytesRead < totalToRead)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("Operation was canceled by the caller.", cancellationToken);
            }

            // If there's still data in the current iteration
            if (_remainingData is not null && _remainingDataOffset < _remainingData.Value.Length)
            {
                int bytesToCopy = Math.Min(totalToRead - bytesRead, _remainingData.Value.Length - _remainingDataOffset);
                _remainingData.Value.Slice(_remainingDataOffset, bytesToCopy)
                             .CopyTo(buffer.Slice(bytesRead, bytesToCopy));

                _remainingDataOffset += bytesToCopy;
                bytesRead += bytesToCopy;
                _position += bytesToCopy;
            }
            else
            {
                // If the first data content was never read, attempt to read it now
                if (_position == 0 && _firstDataContent is not null)
                {
                    _remainingData = _firstDataContent.Data;
                    _remainingDataOffset = 0;
                    continue;
                }

                // Move to the next data content in the async enumerator
                if (!await _enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    _isCompleted = true;
                    break;
                }

                _remainingData = _enumerator.Current.Data;
                _remainingDataOffset = 0;
            }
        }

        return bytesRead;
    }
}

