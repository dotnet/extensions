// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable S4136 // Method overloads should be grouped together - .NET Core overloads grouped in #if NET block below

namespace Microsoft.Extensions.AI;

/// <summary>
/// A <see cref="HostedFileDownloadStream"/> implementation for OpenAI file downloads.
/// </summary>
internal sealed class OpenAIFileDownloadStream : HostedFileDownloadStream
{
    private readonly Stream _innerStream;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAIFileDownloadStream"/> class.
    /// </summary>
    /// <param name="data">The downloaded file data.</param>
    /// <param name="mediaType">The media type of the file.</param>
    /// <param name="fileName">The file name.</param>
    public OpenAIFileDownloadStream(BinaryData data, string? mediaType, string? fileName)
    {
        _innerStream = data.ToStream();
        MediaType = mediaType;
        FileName = fileName;
    }

    /// <inheritdoc />
    public override string? MediaType { get; }

    /// <inheritdoc />
    public override string? FileName { get; }

    /// <inheritdoc />
    public override bool CanRead => _innerStream.CanRead;

    /// <inheritdoc />
    public override bool CanSeek => _innerStream.CanSeek;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override long Length => _innerStream.Length;

    /// <inheritdoc />
    public override long Position
    {
        get => _innerStream.Position;
        set => _innerStream.Position = value;
    }

    /// <inheritdoc />
    public override void Flush() =>
        _innerStream.Flush();

    /// <inheritdoc />
    public override Task FlushAsync(CancellationToken cancellationToken) =>
        _innerStream.FlushAsync(cancellationToken);

    /// <inheritdoc />
    public override int ReadByte() =>
        _innerStream.ReadByte();

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count) =>
        _innerStream.Read(buffer, offset, count);

    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        _innerStream.ReadAsync(buffer, offset, count, cancellationToken);

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) =>
        _innerStream.Seek(offset, origin);

    /// <inheritdoc />
    public override void SetLength(long value) =>
        _innerStream.SetLength(value);

    /// <inheritdoc />
    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) =>
        _innerStream.CopyToAsync(destination, bufferSize, cancellationToken);

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _innerStream.Dispose();
        }

        base.Dispose(disposing);
    }

#if NET
    public override void CopyTo(Stream destination, int bufferSize) => _innerStream.CopyTo(destination, bufferSize);

    /// <inheritdoc />
    public override int Read(Span<byte> buffer) => _innerStream.Read(buffer);

    /// <inheritdoc />
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        _innerStream.ReadAsync(buffer, cancellationToken);

    /// <inheritdoc />
    public override void Write(ReadOnlySpan<byte> buffer) => throw new NotSupportedException();

    /// <inheritdoc />
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        await _innerStream.DisposeAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }
#endif
}
