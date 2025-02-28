// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class DataContentAsyncEnumerableStreamTests
{
    [Fact]
    public void Constructor_InvalidArgs_Throws()
    {
        // Expect ArgumentNullException if source is null.
        Assert.Throws<ArgumentNullException>("dataAsyncEnumerable", () => new DataContentAsyncEnumerableStreamImpl<DataContent>(null!));
    }

    [Fact]
    public async Task ReadAsync_EmptySource_ReturnsEmpty()
    {
        var source = GetAsyncEnumerable(Array.Empty<byte[]>());
        using var stream = new DataContentAsyncEnumerableStreamImpl<DataContent>(source);

        byte[] buffer = new byte[10];
        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        Assert.Equal(0, bytesRead);
    }

    [Fact]
    public async Task ReadAsync_SingleChunk_ReturnsChunk()
    {
        var source = GetAsyncEnumerable(new[] { new byte[] { 1, 2, 3, 4, 5 } });
        using var stream = new DataContentAsyncEnumerableStreamImpl<DataContent>(source);

        byte[] buffer = new byte[10];
        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

        Assert.Equal(5, bytesRead);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 0, 0, 0, 0, 0 }, buffer);
    }

    [Fact]
    public async Task ReadAsync_MultipleChunks_ReturnsConcatenatedChunks()
    {
        var source = GetAsyncEnumerable(new[]
        {
            new byte[] { 1,2,3 },
            new byte[] { 4,5,6 },
            new byte[] { 7,8,9 }
        });
        using var stream = new DataContentAsyncEnumerableStreamImpl<DataContent>(source);

        byte[] buffer = new byte[10];
        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        Assert.Equal(9, bytesRead);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 }, buffer);
    }

    [Fact]
    public async Task ReadAsync_BufferTooSmall_ReturnsPartialChunk()
    {
        var source = GetAsyncEnumerable(new[] { new byte[] { 1, 2, 3, 4, 5 } });
        using var stream = new DataContentAsyncEnumerableStreamImpl<DataContent>(source);

        byte[] buffer = new byte[3];
        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        Assert.Equal(3, bytesRead);
        Assert.Equal(new byte[] { 1, 2, 3 }, buffer);
    }

    [Fact]
    public async Task ReadAsync_BufferTooSmall_MultipleReads()
    {
        var source = GetAsyncEnumerable(new[] { new byte[] { 1, 2, 3, 4, 5 } });
        using var stream = new DataContentAsyncEnumerableStreamImpl<DataContent>(source);

        byte[] buffer = new byte[3];
        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        Assert.Equal(3, bytesRead);
        Assert.Equal(new byte[] { 1, 2, 3 }, buffer);

        bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        Assert.Equal(2, bytesRead);

        // The third byte in the buffer remains from the previous read.
        Assert.Equal(new byte[] { 4, 5, 3 }, buffer);
    }

    [Fact]
    public async Task ReadAsync_WithFirstDataContent_ReturnsFirstDataContentThenSource()
    {
        var firstDataContentBytes = new byte[] { 0, 0, 0, 0, 0 };
        var source = GetAsyncEnumerable(new[] { new byte[] { 1, 2, 3, 4, 5 } });

        // Pass a firstDataContent as a DataContent instance.
        using var stream = new DataContentAsyncEnumerableStreamImpl<DataContent>(source, new DataContent(firstDataContentBytes, "application/octet-stream"));

        byte[] buffer = new byte[10];
        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        Assert.Equal(10, bytesRead);
        Assert.Equal(new byte[] { 0, 0, 0, 0, 0, 1, 2, 3, 4, 5 }, buffer);
    }

    [Fact]
    public void Constructor_WithFirstDataContent_InitializesCorrectly()
    {
        var firstDataContentBytes = new byte[] { 0, 0, 0, 0, 0 };
        var source = GetAsyncEnumerable(new[] { new byte[] { 1, 2, 3, 4, 5 } });
        using var stream = new DataContentAsyncEnumerableStreamImpl<DataContent>(source, new DataContent(firstDataContentBytes, "application/octet-stream"));
        Assert.NotNull(stream);
    }

    [Fact]
    public async Task ReadAsync_EmptySource_WithFirstDataContent_ReturnsFirstDataContent()
    {
        var firstDataContentBytes = new byte[] { 0, 0, 0, 0, 0 };
        var source = GetAsyncEnumerable(Array.Empty<byte[]>());
        using var stream = new DataContentAsyncEnumerableStreamImpl<DataContent>(source, new DataContent(firstDataContentBytes, "application/octet-stream"));

        byte[] buffer = new byte[10];
        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        Assert.Equal(5, bytesRead);
        Assert.Equal(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, buffer);
    }

    private static async IAsyncEnumerable<DataContent> GetAsyncEnumerable(IEnumerable<byte[]> chunks)
    {
        foreach (var chunk in chunks)
        {
            await Task.Yield();
            yield return new DataContent(chunk, "application/octet-stream");
        }
    }

    /// <summary>
    /// Utility class to stream <see cref="IAsyncEnumerable{T}"/> data content as a <see cref="Stream"/>.
    /// </summary>
    /// <typeparam name="T">The type of data content to stream.</typeparam>
#if !NET8_0_OR_GREATER
    internal sealed class DataContentAsyncEnumerableStreamImpl<T> : Stream, IAsyncDisposable
#else
    internal sealed class DataContentAsyncEnumerableStreamImpl<T> : Stream
#endif
        where T : DataContent
    {
        private readonly IAsyncEnumerator<T> _enumerator;
        private bool _isCompleted;
        private byte[] _remainingData;
        private int _remainingDataOffset;
        private long _position;
        private T? _firstDataContent;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataContentAsyncEnumerableStreamImpl{T}"/> class, where T is <see cref="DataContent"/>.
        /// </summary>
        /// <param name="dataAsyncEnumerable">The async enumerable to stream.</param>
        /// <param name="firstDataContent">The first chunk of data to reconsider when reading.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <remarks>
        /// <paramref name="firstDataContent"/> needs to be considered back in the stream if <paramref name="dataAsyncEnumerable"/> was iterated before creating the stream.
        /// This can happen to check if the first enumerable item contains data or is just a reference only content.
        /// </remarks>
        internal DataContentAsyncEnumerableStreamImpl(IAsyncEnumerable<T> dataAsyncEnumerable, T? firstDataContent = null, CancellationToken cancellationToken = default)
        {
            if (dataAsyncEnumerable is null)
            {
                throw new ArgumentNullException(nameof(dataAsyncEnumerable));
            }

            _enumerator = dataAsyncEnumerable.GetAsyncEnumerator(cancellationToken);
            _remainingData = Array.Empty<byte>();
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
#if NET8_0_OR_GREATER
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => ReadAsync(buffer, cancellationToken).AsTask();
#else
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_isCompleted)
            {
                return 0;
            }

            int bytesRead = 0;

            while (bytesRead < count)
            {
                if (_remainingDataOffset < _remainingData.Length)
                {
                    int bytesToCopy = Math.Min(count - bytesRead, _remainingData.Length - _remainingDataOffset);
                    Array.Copy(_remainingData, _remainingDataOffset, buffer, offset + bytesRead, bytesToCopy);
                    _remainingDataOffset += bytesToCopy;
                    bytesRead += bytesToCopy;
                    _position += bytesToCopy;
                }
                else
                {
                    // Special case when the first chunk was skipped and needs to be read
                    if (_position == 0 && _firstDataContent is not null && _firstDataContent.Data.HasValue)
                    {
                        _remainingData = _firstDataContent.Data.Value.ToArray();
                        _remainingDataOffset = 0;

                        continue;
                    }

                    if (!await _enumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        _isCompleted = true;
                        break;
                    }

                    if (!_enumerator.Current.Data.HasValue)
                    {
                        _isCompleted = true;
                        break;
                    }

                    _remainingData = _enumerator.Current.Data.Value.ToArray();
                    _remainingDataOffset = 0;
                }
            }

            return bytesRead;
        }
#endif

#if NET8_0_OR_GREATER
        /// <inheritdoc/>
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_isCompleted)
            {
                return 0;
            }

            int bytesRead = 0;
            int totalToRead = buffer.Length;

            while (bytesRead < totalToRead)
            {
                // If there's still data in the current chunk
                if (_remainingDataOffset < _remainingData.Length)
                {
                    int bytesToCopy = Math.Min(totalToRead - bytesRead, _remainingData.Length - _remainingDataOffset);
                    _remainingData.AsSpan(_remainingDataOffset, bytesToCopy)
                                  .CopyTo(buffer.Span.Slice(bytesRead, bytesToCopy));

                    _remainingDataOffset += bytesToCopy;
                    bytesRead += bytesToCopy;
                    _position += bytesToCopy;
                }
                else
                {
                    // If the first chunk was never read, attempt to read it now
                    if (_position == 0 && _firstDataContent is not null && _firstDataContent.Data.HasValue)
                    {
                        _remainingData = _firstDataContent.Data.Value.ToArray();
                        _remainingDataOffset = 0;
                        continue;
                    }

                    // Move to the next chunk in the async enumerator
                    if (!await _enumerator.MoveNextAsync().ConfigureAwait(false) ||
                        !_enumerator.Current.Data.HasValue)
                    {
                        _isCompleted = true;
                        break;
                    }

                    _remainingData = _enumerator.Current.Data.Value.ToArray();
                    _remainingDataOffset = 0;
                }
            }

            return bytesRead;
        }
#endif

#if NET8_0_OR_GREATER
        /// <inheritdoc/>
        public override async ValueTask DisposeAsync()
        {
            await _enumerator.DisposeAsync().ConfigureAwait(false);

            await base.DisposeAsync().ConfigureAwait(false);
        }
#else
        public async ValueTask DisposeAsync()
        {
            await _enumerator.DisposeAsync().ConfigureAwait(false);

            Dispose();
        }
#endif

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                var task = Task.Run(_enumerator.DisposeAsync);
            }

            base.Dispose(disposing);
        }
    }
}
