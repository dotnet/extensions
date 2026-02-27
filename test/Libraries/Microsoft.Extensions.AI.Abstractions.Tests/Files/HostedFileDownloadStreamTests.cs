// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable MEAI001

using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class HostedFileDownloadStreamTests
{
    [Fact]
    public void Defaults_ReturnNull()
    {
        using var stream = new MinimalDownloadStream([1, 2, 3]);
        Assert.Null(stream.MediaType);
        Assert.Null(stream.FileName);
    }

    [Fact]
    public async Task ToDataContentAsync_BuffersStreamContent()
    {
        var data = new byte[] { 10, 20, 30, 40, 50 };
        using var stream = new MetadataDownloadStream(data, "application/json", "data.json");
        var content = await stream.ToDataContentAsync();
        Assert.Equal(data, content.Data.ToArray());
        Assert.Equal("application/json", content.MediaType);
        Assert.Equal("data.json", content.Name);
    }

    [Fact]
    public async Task ToDataContentAsync_NullMediaType_DefaultsToOctetStream()
    {
        var data = new byte[] { 1, 2 };
        using var stream = new MinimalDownloadStream(data);
        var content = await stream.ToDataContentAsync();
        Assert.Equal(data, content.Data.ToArray());
        Assert.Equal("application/octet-stream", content.MediaType);
        Assert.Null(content.Name);
    }

    [Fact]
    public async Task ToDataContentAsync_EmptyStream_ReturnsEmptyData()
    {
        using var stream = new MinimalDownloadStream([]);
        var content = await stream.ToDataContentAsync();
        Assert.Empty(content.Data.ToArray());
    }

    /// <summary>
    /// Minimal implementation that does not override MediaType or FileName, testing the default behavior.
    /// </summary>
    private sealed class MinimalDownloadStream : HostedFileDownloadStream
    {
        private readonly MemoryStream _inner;

        public MinimalDownloadStream(byte[] data)
        {
            _inner = new MemoryStream(data);
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => _inner.Position = value; }
        public override void Flush() => _inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => _inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Implementation that provides MediaType and FileName metadata.
    /// </summary>
    private sealed class MetadataDownloadStream : HostedFileDownloadStream
    {
        private readonly MemoryStream _inner;

        public MetadataDownloadStream(byte[] data, string? mediaType, string? fileName)
        {
            _inner = new MemoryStream(data);
            MediaType = mediaType;
            FileName = fileName;
        }

        public override string? MediaType { get; }
        public override string? FileName { get; }
        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => _inner.Position = value; }
        public override void Flush() => _inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => _inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
