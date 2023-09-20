// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Diagnostics.Logging;

internal static class HttpRequestBodyReader
{
    internal const string ReadCancelled = "[read-cancelled]";

    private static readonly ReadOnlySequence<byte> _readCancelled = new(Encoding.UTF8.GetBytes(ReadCancelled));

    public static async ValueTask<ReadOnlySequence<byte>> ReadBodyAsync(
        this HttpRequest request,
        TimeSpan readTimeout,
        int readSizeLimit,
        CancellationToken token)
    {
        using var joinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        joinedTokenSource.CancelAfter(readTimeout);

        try
        {
            /*
             * We enable buffering with max threshold and max limit.
             * - Max threshold practically means we don't start writing into a file.
             * - Max limit practically means we won't get a capacity exception.
            */
            request.EnableBuffering(int.MaxValue, long.MaxValue);

            return await request.BodyReader.ReadAsync(readSizeLimit, joinedTokenSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Token source hides triggering token (https://github.com/dotnet/runtime/issues/22172)
            if (!token.IsCancellationRequested)
            {
                return _readCancelled;
            }

            throw;
        }
        finally
        {
            if (request.Body.CanSeek)
            {
                _ = request.Body.Seek(0, SeekOrigin.Begin);
            }
        }
    }
}
