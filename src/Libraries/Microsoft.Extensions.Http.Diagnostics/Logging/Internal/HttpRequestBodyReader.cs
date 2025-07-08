// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Frozen;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.Logging.Internal;

internal sealed class HttpRequestBodyReader
{
    /// <summary>
    /// Exposed for testing purposes.
    /// </summary>
    internal readonly TimeSpan RequestReadTimeout;

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
            return Constants.ReadCancelledByTimeout;
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
        var buffer = ArrayPool<byte>.Shared.Rent(readLimit);
        try
        {
#if NET
            var read = await streamToReadFrom.ReadAsync(buffer.AsMemory(0, readLimit), cancellationToken).ConfigureAwait(false);
#else
            var read = await streamToReadFrom.ReadAsync(buffer, 0, readLimit, cancellationToken).ConfigureAwait(false);
#endif
            return Encoding.UTF8.GetString(buffer, 0, read);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            streamToReadFrom.Seek(0, SeekOrigin.Begin);
        }
    }
}
