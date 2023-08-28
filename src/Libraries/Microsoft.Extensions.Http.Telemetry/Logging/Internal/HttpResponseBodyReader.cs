// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using Microsoft.IO;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Internal;

internal sealed class HttpResponseBodyReader
{
    /// <summary>
    /// Exposed for testing purposes.
    /// </summary>
    internal readonly TimeSpan ResponseReadTimeout;

    private static readonly ObjectPool<BufferWriter<byte>> _bufferWriterPool = BufferWriterPool.SharedBufferWriterPool;
    private readonly FrozenSet<string> _readableResponseContentTypes;
    private readonly int _responseReadLimit;

    private readonly RecyclableMemoryStreamManager _streamManager;

    public HttpResponseBodyReader(LoggingOptions responseOptions, IDebuggerState? debugger = null)
    {
        _streamManager = new RecyclableMemoryStreamManager();
        _readableResponseContentTypes = responseOptions.ResponseBodyContentTypes.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        _responseReadLimit = responseOptions.BodySizeLimit;

        debugger ??= DebuggerState.System;

        ResponseReadTimeout = debugger.IsAttached
            ? Timeout.InfiniteTimeSpan
            : responseOptions.BodyReadTimeout;
    }

    public ValueTask<string> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var contentType = response.Content.Headers.ContentType;
        if (contentType == null)
        {
            return new(Constants.NoContent);
        }

        if (!_readableResponseContentTypes.Covers(contentType.MediaType!))
        {
            return new(Constants.UnreadableContent);
        }

        return ReadFromStreamWithTimeoutAsync(response, ResponseReadTimeout, _responseReadLimit, _streamManager,
            cancellationToken).Preserve();
    }

    private static async ValueTask<string> ReadFromStreamAsync(HttpResponseMessage response, int readSizeLimit,
        RecyclableMemoryStreamManager streamManager, CancellationToken cancellationToken)
    {
#if NET5_0_OR_GREATER
        var streamToReadFrom = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#else
        var streamToReadFrom = await response.Content.ReadAsStreamAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
#endif

        var bufferWriter = _bufferWriterPool.Get();
        var memory = bufferWriter.GetMemory(readSizeLimit).Slice(0, readSizeLimit);
#if !NETCOREAPP3_1_OR_GREATER
        byte[] buffer = memory.ToArray();
#endif
        try
        {
#if NETCOREAPP3_1_OR_GREATER
            var charsWritten = await streamToReadFrom.ReadAsync(memory, cancellationToken).ConfigureAwait(false);
            bufferWriter.Advance(charsWritten);
            return Encoding.UTF8.GetString(memory.Slice(0, charsWritten).Span);
#else
            var charsWritten = await streamToReadFrom.ReadAsync(buffer, 0, readSizeLimit, cancellationToken).ConfigureAwait(false);
            bufferWriter.Advance(charsWritten);
            return Encoding.UTF8.GetString(buffer.AsMemory(0, charsWritten).ToArray());
#endif
        }
        finally
        {
            if (streamToReadFrom.CanSeek)
            {
                streamToReadFrom.Seek(0, SeekOrigin.Begin);
            }
            else
            {
                var freshStream = streamManager.GetStream();
#if NETCOREAPP3_1_OR_GREATER
                var remainingSpace = memory.Slice(bufferWriter.WrittenCount, memory.Length - bufferWriter.WrittenCount);
                var writtenCount = await streamToReadFrom.ReadAsync(remainingSpace, cancellationToken)
                    .ConfigureAwait(false);

                await freshStream.WriteAsync(memory.Slice(0, writtenCount + bufferWriter.WrittenCount), cancellationToken)
                    .ConfigureAwait(false);
#else
                var writtenCount = await streamToReadFrom.ReadAsync(buffer, bufferWriter.WrittenCount,
                    buffer.Length - bufferWriter.WrittenCount, cancellationToken).ConfigureAwait(false);

                await freshStream.WriteAsync(buffer, 0, writtenCount + bufferWriter.WrittenCount, cancellationToken).ConfigureAwait(false);
#endif
                freshStream.Seek(0, SeekOrigin.Begin);

                var newContent = new StreamContent(freshStream);

                foreach (var header in response.Content.Headers)
                {
                    _ = newContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                response.Content = newContent;
            }

            _bufferWriterPool.Return(bufferWriter);
        }
    }

    private static async ValueTask<string> ReadFromStreamWithTimeoutAsync(HttpResponseMessage response, TimeSpan readTimeout,
        int readSizeLimit, RecyclableMemoryStreamManager streamManager, CancellationToken cancellationToken)
    {
        using var joinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        joinedTokenSource.CancelAfter(readTimeout);

        try
        {
            return await ReadFromStreamAsync(response, readSizeLimit, streamManager, joinedTokenSource.Token)
                .ConfigureAwait(false);
        }

        // when readTimeout occurred:
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Constants.ReadCancelled;
        }
    }
}
