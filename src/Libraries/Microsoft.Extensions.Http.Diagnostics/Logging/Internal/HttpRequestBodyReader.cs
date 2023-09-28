// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#if NETCOREAPP3_1_OR_GREATER
using Microsoft.Extensions.ObjectPool;
#endif
using Microsoft.Shared.Diagnostics;
#if NETCOREAPP3_1_OR_GREATER
using Microsoft.Shared.Pools;
#else
using System.Buffers;
#endif

namespace Microsoft.Extensions.Http.Logging.Internal;

internal sealed class HttpRequestBodyReader
{
    /// <summary>
    /// Exposed for testing purposes.
    /// </summary>
    internal readonly TimeSpan RequestReadTimeout;

#if NETCOREAPP3_1_OR_GREATER
    private static readonly ObjectPool<BufferWriter<byte>> _bufferWriterPool = BufferWriterPool.SharedBufferWriterPool;
#endif
    private readonly FrozenSet<string> _readableRequestContentTypes;
    private readonly int _requestReadLimit;

    public HttpRequestBodyReader(LoggingOptions requestOptions, IDebuggerState? debugger = null)
    {
        _readableRequestContentTypes = requestOptions.RequestBodyContentTypes.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        debugger ??= DebuggerState.System;
        _requestReadLimit = requestOptions.BodySizeLimit;

        RequestReadTimeout = debugger.IsAttached
            ? Timeout.InfiniteTimeSpan
            : requestOptions.BodyReadTimeout;
    }

    public ValueTask<string> ReadAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content == null || request.Method == HttpMethod.Get)
        {
            return new(string.Empty);
        }

        var contentType = request.Content.Headers.ContentType;
        if (contentType == null)
        {
            return new(Constants.NoContent);
        }

        if (!_readableRequestContentTypes.Covers(contentType.MediaType))
        {
            return new(Constants.UnreadableContent);
        }

        return ReadFromStreamWithTimeoutAsync(request, RequestReadTimeout, _requestReadLimit, cancellationToken).Preserve();
    }

    private static async ValueTask<string> ReadFromStreamWithTimeoutAsync(HttpRequestMessage request,
        TimeSpan readTimeout, int readSizeLimit, CancellationToken cancellationToken)
    {
        using var joinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        joinedTokenSource.CancelAfter(readTimeout);

        try
        {
            return await ReadFromStreamAsync(request, readSizeLimit, joinedTokenSource.Token).ConfigureAwait(false);
        }

        // when readTimeout occurred:
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Constants.ReadCancelled;
        }
    }

    private static async ValueTask<string> ReadFromStreamAsync(HttpRequestMessage request, int readSizeLimit,
        CancellationToken cancellationToken)
    {
#if NET5_0_OR_GREATER
        var streamToReadFrom = await request.Content!.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#else
        var streamToReadFrom = await request.Content.ReadAsStreamAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
#endif

        var readLimit = Math.Min(readSizeLimit, (int)streamToReadFrom.Length);
#if NETCOREAPP3_1_OR_GREATER
        var bufferWriter = _bufferWriterPool.Get();
        try
        {
            var memory = bufferWriter.GetMemory(readLimit).Slice(0, readLimit);
            var charsWritten = await streamToReadFrom.ReadAsync(memory, cancellationToken).ConfigureAwait(false);

            return Encoding.UTF8.GetString(memory[..charsWritten].Span);
        }
        finally
        {
            _bufferWriterPool.Return(bufferWriter);
            streamToReadFrom.Seek(0, SeekOrigin.Begin);
        }

#else
        var buffer = ArrayPool<byte>.Shared.Rent(readLimit);
        try
        {
            _ = await streamToReadFrom.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
            return Encoding.UTF8.GetString(buffer.AsSpan(0, readLimit).ToArray());
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            streamToReadFrom.Seek(0, SeekOrigin.Begin);
        }
#endif
    }
}
