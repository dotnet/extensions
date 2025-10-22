// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DataIngestion;

/// <summary>
/// Writes chunks to a destination.
/// </summary>
/// <typeparam name="T">The type of the chunk content.</typeparam>
public abstract class IngestionChunkWriter<T> : IDisposable
{
    /// <summary>
    /// Writes chunks asynchronously.
    /// </summary>
    /// <param name="chunks">The chunks to write.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    public abstract Task WriteAsync(IAsyncEnumerable<IngestionChunk<T>> chunks, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disposes the writer and releases all associated resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the writer.
    /// </summary>
    /// <param name="disposing">true if called from dispose, false if called from finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
    }
}
