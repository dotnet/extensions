// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Extensions.DataIngestion;

/// <summary>
/// Processes chunks in a pipeline.
/// </summary>
/// <typeparam name="T">The type of the chunk content.</typeparam>
public abstract class IngestionChunkProcessor<T>
{
    /// <summary>
    /// Processes chunks asynchronously.
    /// </summary>
    /// <param name="chunks">The chunks to process.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The processed chunks.</returns>
    public abstract IAsyncEnumerable<IngestionChunk<T>> ProcessAsync(IAsyncEnumerable<IngestionChunk<T>> chunks, CancellationToken cancellationToken = default);
}
