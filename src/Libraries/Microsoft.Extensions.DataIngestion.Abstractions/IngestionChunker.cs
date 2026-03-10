// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Extensions.DataIngestion;

/// <summary>
/// Splits an <see cref="IngestionDocument"/> into chunks.
/// </summary>
/// <typeparam name="T">The type of the chunk content.</typeparam>
public abstract class IngestionChunker<T>
{
    /// <summary>
    /// Splits a document into chunks asynchronously.
    /// </summary>
    /// <param name="document">The document to split.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The chunks created from the document.</returns>
    public abstract IAsyncEnumerable<IngestionChunk<T>> ProcessAsync(IngestionDocument document, CancellationToken cancellationToken = default);
}
