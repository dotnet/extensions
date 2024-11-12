// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.IO;
using System.IO.Pipelines;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.Logging.Internal;

internal sealed class HttpResponseBodyReader
{
    /// <summary>
    /// Exposed for testing purposes.
    /// </summary>
    internal readonly TimeSpan ResponseReadTimeout;

    private const int ChunkSize = 8 * 1024;
    private readonly FrozenSet<string> _readableResponseContentTypes;
    private readonly int _responseReadLimit;

    public HttpResponseBodyReader(LoggingOptions responseOptions, IDebuggerState? debugger = null)
    {
        _ = Throw.IfNull(responseOptions);

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

        return ReadFromStreamWithTimeoutAsync(response, ResponseReadTimeout, _responseReadLimit, cancellationToken).Preserve();
    }

    private static async ValueTask<string> ReadFromStreamWithTimeoutAsync(HttpResponseMessage response, TimeSpan readTimeout, int readSizeLimit, CancellationToken cancellationToken)
    {
        using var joinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        joinedTokenSource.CancelAfter(readTimeout);

        // TimeSpan.Zero cannot be set from user's code as
        // validation prevents values less than one millisecond
        // However, this is useful during unit tests
        if (readTimeout <= TimeSpan.Zero)
        {
            // cancel immediately, async cancel not required in tests
#pragma warning disable CA1849 // Call async methods when in an async method
            joinedTokenSource.Cancel();
#pragma warning restore CA1849 // Call async methods when in an async method
        }

        try
        {
            return await ReadFromStreamAsync(response, readSizeLimit, joinedTokenSource.Token).ConfigureAwait(false);
        }

        // when readTimeout occurred: joined token source is cancelled and cancellationToken is not
        catch (OperationCanceledException) when (joinedTokenSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            return Constants.ReadCancelled;
        }
    }

    private static async ValueTask<string> ReadFromStreamAsync(HttpResponseMessage response, int readSizeLimit, CancellationToken cancellationToken)
    {
#if NET6_0_OR_GREATER
        var streamToReadFrom = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#else
        var streamToReadFrom = await response.Content.ReadAsStreamAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
#endif

        var pipe = new Pipe();

        var bufferedString = await BufferStreamAndWriteToPipeAsync(streamToReadFrom, pipe.Writer, readSizeLimit, cancellationToken).ConfigureAwait(false);

        // if stream is seekable we can just rewind it and return the buffered string
        if (streamToReadFrom.CanSeek)
        {
            streamToReadFrom.Seek(0, SeekOrigin.Begin);

            await pipe.Reader.CompleteAsync().ConfigureAwait(false);

            return bufferedString;
        }

        // if stream is not seekable we need to write the rest of the stream to the pipe
        // and create a new response content with the pipe reader as stream
        _ = Task.Run(async () =>
        {
            await WriteStreamToPipeAsync(streamToReadFrom, pipe.Writer, cancellationToken).ConfigureAwait(false);
        }, CancellationToken.None).ConfigureAwait(false);

        // use the pipe reader as stream for the new content
        var newContent = new StreamContent(pipe.Reader.AsStream());
        foreach (var header in response.Content.Headers)
        {
            _ = newContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        response.Content = newContent;

        return bufferedString;
    }

#if NET6_0_OR_GREATER
    private static async Task<string> BufferStreamAndWriteToPipeAsync(Stream stream, PipeWriter writer, int bufferSize, CancellationToken cancellationToken)
    {
        var memory = writer.GetMemory(bufferSize)[..bufferSize];

#if NET8_0_OR_GREATER
        int bytesRead = await stream.ReadAtLeastAsync(memory, bufferSize, false, cancellationToken).ConfigureAwait(false);
#else
        int bytesRead = 0;
        while (bytesRead < bufferSize)
        {
            int read = await stream.ReadAsync(memory.Slice(bytesRead), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            bytesRead += read;
        }
#endif

        if (bytesRead == 0)
        {
            return string.Empty;
        }

        writer.Advance(bytesRead);

        return Encoding.UTF8.GetString(memory[..bytesRead].Span);
    }

    private static async Task WriteStreamToPipeAsync(Stream stream, PipeWriter writer, CancellationToken cancellationToken)
    {
        while (true)
        {
            Memory<byte> memory = writer.GetMemory(ChunkSize)[..ChunkSize];

            int bytesRead = await stream.ReadAsync(memory, cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                break;
            }

            writer.Advance(bytesRead);

            FlushResult result = await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
            if (result.IsCompleted)
            {
                break;
            }
        }

        await writer.CompleteAsync().ConfigureAwait(false);
    }
#else
    private static async Task<string> BufferStreamAndWriteToPipeAsync(Stream stream, PipeWriter writer, int bufferSize, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();

        int bytesRead = 0;

        while (bytesRead < bufferSize)
        {
            var chunkSize = Math.Min(ChunkSize, bufferSize - bytesRead);

            var memory = writer.GetMemory(chunkSize).Slice(0, chunkSize);

            byte[] buffer = memory.ToArray();

            int read = await stream.ReadAsync(buffer, 0, chunkSize, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            bytesRead += read;

            buffer.CopyTo(memory);

            writer.Advance(read);

            _ = sb.Append(Encoding.UTF8.GetString(buffer.AsMemory(0, read).ToArray()));
        }

        return sb.ToString();
    }

    private static async Task WriteStreamToPipeAsync(Stream stream, PipeWriter writer, CancellationToken cancellationToken)
    {
        while (true)
        {
            Memory<byte> memory = writer.GetMemory(ChunkSize).Slice(0, ChunkSize);
            byte[] buffer = memory.ToArray();

            int bytesRead = await stream.ReadAsync(buffer, 0, ChunkSize, cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                break;
            }

            FlushResult result = await writer.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
            if (result.IsCompleted)
            {
                break;
            }
        }

        await writer.CompleteAsync().ConfigureAwait(false);
    }
#endif
}
