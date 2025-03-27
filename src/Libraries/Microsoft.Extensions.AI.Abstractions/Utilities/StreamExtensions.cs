// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Shared.Diagnostics;

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods

namespace Microsoft.Extensions.AI;

/// <summary>Provides extension methods for <see cref="Stream"/>.</summary>
public static class StreamExtensions
{
    /// <summary>Converts a <see cref="Stream"/> to an <see cref="IAsyncEnumerable{DataContent}"/>.</summary>
    /// <param name="stream">The <see cref="Stream"/> to convert.</param>
    /// <param name="mediaType">The optional media type of the audio stream.</param>
    /// <param name="bufferSize">The optional buffer size to use when reading from the audio stream. The default is 4096.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>An <see cref="IAsyncEnumerable{DataContent}"/>.</returns>
    public static async IAsyncEnumerable<DataContent> ToAsyncEnumerable(
        this Stream stream,
        string mediaType = "audio/*",
        int bufferSize = 4096,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        int bytesRead;
#if NET8_0_OR_GREATER
        Memory<byte> buffer = new byte[bufferSize];
        while ((bytesRead = await Throw.IfNull(stream).ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            yield return new DataContent(buffer.Slice(0, bytesRead), mediaType)!;
        }
#else
        var buffer = new byte[bufferSize];
        while ((bytesRead = await Throw.IfNull(stream).ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
        {
            byte[] chunk = new byte[bytesRead];
            Array.Copy(buffer, 0, chunk, 0, bytesRead);
            yield return new DataContent((ReadOnlyMemory<byte>)chunk, mediaType)!;
        }
#endif
    }
}
