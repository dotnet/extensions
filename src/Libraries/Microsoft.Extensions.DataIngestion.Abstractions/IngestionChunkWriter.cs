// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DataIngestion;

public abstract class IngestionChunkWriter<T> : IDisposable
{
    public abstract Task WriteAsync(IAsyncEnumerable<IngestionChunk<T>> chunks, CancellationToken cancellationToken = default);

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
    }
}
