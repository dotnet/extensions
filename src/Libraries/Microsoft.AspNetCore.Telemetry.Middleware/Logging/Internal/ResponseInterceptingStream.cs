// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Shared.Pools;

namespace Microsoft.AspNetCore.Telemetry.Http.Logging;

/// <summary>
/// Intercepts data being written to stream and writes its copy to another data structure.
/// </summary>
/// <remarks>
/// Copied from <see href="https://github.com/dotnet/aspnetcore/blob/main/src/Middleware/HttpLogging/src/ResponseBufferingStream.cs">ASP .NET</see> and adjusted to R9 needs.
/// </remarks>
internal sealed class ResponseInterceptingStream : Stream, IHttpResponseBodyFeature
{
    private const bool LeavePipeWriterOpened = true;
    private static readonly StreamPipeWriterOptions _pipeWriterOptions
        = new(leaveOpen: LeavePipeWriterOpened);

    private PipeWriter? _pipeAdapter;

    public PipeWriter Writer => _pipeAdapter ??= PipeWriter.Create(this, _pipeWriterOptions);

    public Stream Stream => this;

    public override bool CanSeek => InterceptedStream.CanSeek;

    public override bool CanRead => InterceptedStream.CanRead;

    public override bool CanWrite => InterceptedStream.CanWrite;

    public override long Length => InterceptedStream.Length;

    public override long Position
    {
        get => InterceptedStream.Position;
        set => InterceptedStream.Position = value;
    }

    public override int WriteTimeout
    {
        get => InterceptedStream.WriteTimeout;
        set => InterceptedStream.WriteTimeout = value;
    }

    /// <remarks>
    /// Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// Justification: I need parameterless ctor to use pooling on this object. I don't want to declare it as nullable because
    /// using current pattern it is never null after initialization.
    /// </remarks>
#pragma warning disable CS8618
    public ResponseInterceptingStream()
    {
    }
#pragma warning restore CS8618

    public ResponseInterceptingStream(
        Stream interceptedStream,
        IHttpResponseBodyFeature responseBodyFeature,
        BufferWriter<byte> bufferWriter,
        int interceptedValueWriteLimit)
    {
        InterceptedStream = interceptedStream;
        InnerBodyFeature = responseBodyFeature;
        InterceptedValueBuffer = bufferWriter;
        InterceptedValueWriteLimit = interceptedValueWriteLimit;
    }

    internal Stream InterceptedStream { get; set; }

    internal IHttpResponseBodyFeature InnerBodyFeature { get; set; }

    internal int InterceptedValueWriteLimit { get; set; }

    internal BufferWriter<byte> InterceptedValueBuffer { get; set; }

    public override void Flush()
    {
        InterceptedStream.Flush();
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return InterceptedStream.FlushAsync(cancellationToken);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return InterceptedStream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        InterceptedStream.SetLength(value);
    }

#if NET5_0_OR_GREATER
    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return InterceptedStream.BeginWrite(buffer, offset, count, callback, state);
    }
#else
    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object? state)
    {
        return InterceptedStream.BeginWrite(buffer, offset, count, callback, state);
    }
#endif

    public override void EndWrite(IAsyncResult asyncResult)
    {
        InterceptedStream.EndWrite(asyncResult);
    }

    public override int Read(Span<byte> buffer)
    {
        return InterceptedStream.Read(buffer);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return InterceptedStream.Read(buffer, offset, count);
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return InterceptedStream.ReadAsync(buffer, cancellationToken);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return InterceptedStream.ReadAsync(buffer, offset, count, cancellationToken);
    }

#if NET5_0_OR_GREATER
    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return InterceptedStream.BeginRead(buffer, offset, count, callback, state);
    }
#else
    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object? state)
    {
        return InterceptedStream.BeginRead(buffer, offset, count, callback, state);
    }
#endif

    public override int EndRead(IAsyncResult asyncResult)
    {
        return InterceptedStream.EndRead(asyncResult);
    }

    public override void CopyTo(Stream destination, int bufferSize)
    {
        InterceptedStream.CopyTo(destination, bufferSize);
    }

    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        return InterceptedStream.CopyToAsync(destination, bufferSize, cancellationToken);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync().ConfigureAwait(false);
        await InterceptedStream.DisposeAsync().ConfigureAwait(false);
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        return InnerBodyFeature.StartAsync(cancellationToken);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        Write(buffer.AsSpan(offset, count));
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        var valueToWriteUntilLimit = InterceptedValueWriteLimit - InterceptedValueBuffer.WrittenCount;
        var innerCount = Math.Min(valueToWriteUntilLimit, buffer.Length);

        InterceptedValueBuffer.Write(buffer.Slice(0, innerCount));
        InterceptedStream.Write(buffer);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return WriteAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var valueToWriteUntilLimit = InterceptedValueWriteLimit - InterceptedValueBuffer.WrittenCount;
        var innerCount = Math.Min(valueToWriteUntilLimit, buffer.Length);

        InterceptedValueBuffer.Write(buffer.Span.Slice(0, innerCount));

        return InterceptedStream.WriteAsync(buffer, cancellationToken);
    }

    public void DisableBuffering()
    {
        InnerBodyFeature.DisableBuffering();
    }

    public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken = default)
    {
        return InnerBodyFeature.SendFileAsync(path, offset, count, cancellationToken);
    }

    public Task CompleteAsync()
    {
        return InnerBodyFeature.CompleteAsync();
    }

    internal ReadOnlyMemory<byte> GetInterceptedSequence() => InterceptedValueBuffer.WrittenMemory;
}
