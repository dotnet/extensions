// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
#pragma warning disable SA1202 // Elements should be ordered by access

namespace Microsoft.Extensions.AI;

/// <summary>
/// Utility class to stream <see cref="IAsyncEnumerable{T}"/> data content as a <see cref="Stream"/>.
/// </summary>
internal sealed class DataContentAsyncEnumerableStream : Stream, IAsyncDisposable
{
    private readonly IAsyncEnumerator<DataContent> _enumerator;
    private ReadOnlyMemory<byte> _current;
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
    internal DataContentAsyncEnumerableStream(
        IAsyncEnumerable<DataContent> dataAsyncEnumerable, DataContent? firstDataContent = null, CancellationToken cancellationToken = default)
    {
        _enumerator = Throw.IfNull(dataAsyncEnumerable).GetAsyncEnumerator(cancellationToken);
        _firstDataContent = firstDataContent;
        _current = Memory<byte>.Empty;
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
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <inheritdoc/>
    public override void Flush()
    {
    }

    public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <inheritdoc/>
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count) =>
        ReadAsync(buffer, offset, count).GetAwaiter().GetResult();

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();

    /// <inheritdoc/>
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

#if NET
    /// <inheritdoc/>
    public override
#else
    internal
#endif
    async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (buffer.IsEmpty)
        {
            return 0;
        }

        while (_current.IsEmpty)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_firstDataContent is not null)
            {
                _current = _firstDataContent.Data;
                _firstDataContent = null;
                continue;
            }

            if (!await _enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                return 0;
            }

            _current = _enumerator.Current.Data;
        }

        int toCopy = Math.Min(buffer.Length, _current.Length);
        _current.Slice(0, toCopy).CopyTo(buffer);
        _current = _current.Slice(toCopy);
        return toCopy;
    }

#if NET
    /// <inheritdoc/>
    public override void CopyTo(Stream destination, int bufferSize) =>
        CopyToAsync(destination, bufferSize, CancellationToken.None).GetAwaiter().GetResult();

    /// <inheritdoc/>
    public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(destination);

        if (!_current.IsEmpty)
        {
            await destination.WriteAsync(_current, cancellationToken).ConfigureAwait(false);
            _current = Memory<byte>.Empty;
        }

        if (_firstDataContent is not null)
        {
            await destination.WriteAsync(_firstDataContent.Data, cancellationToken).ConfigureAwait(false);
            _firstDataContent = null;
        }

        while (await _enumerator.MoveNextAsync().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await destination.WriteAsync(_enumerator.Current.Data, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public override async ValueTask DisposeAsync()
    {
        await _enumerator.DisposeAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }
#else
    /// <inheritdoc/>
    public ValueTask DisposeAsync() => _enumerator.DisposeAsync();

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        _enumerator.DisposeAsync().AsTask().GetAwaiter().GetResult();
        base.Dispose(disposing);
    }
#endif
}

