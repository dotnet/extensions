// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Extensions.DataIngestion;

public abstract class IngestionChunker<T>
{
    public abstract IAsyncEnumerable<IngestionChunk<T>> ProcessAsync(IngestionDocument document, CancellationToken cancellationToken = default);
}
